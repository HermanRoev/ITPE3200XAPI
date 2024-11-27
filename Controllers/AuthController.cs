using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ITPE3200XAPI.Models;
using ITPE3200XAPI.DTOs.Auth;
using System;
using System.Linq;

namespace ITPE3200XAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        // 1. Register User
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Check if the username or email already exists
            if (await _userManager.FindByNameAsync(registerDto.Username) != null)
            {
                return BadRequest(new { message = "Username is already taken." });
            }

            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                return BadRequest(new { message = "Email is already taken." });
            }

            // Create the user object
            var user = new ApplicationUser
            {
                UserName = registerDto.Username,
                Email = registerDto.Email
            };

            // Attempt to create the user
            var createResult = await _userManager.CreateAsync(user, registerDto.Password);
            if (!createResult.Succeeded)
            {
                // Return validation errors if user creation fails
                var errors = createResult.Errors.Select(e => new { e.Code, e.Description }).ToList();
                return BadRequest(errors);
            }

            // Attempt to log in the user
            var signInResult = await _signInManager.PasswordSignInAsync(user.UserName, registerDto.Password, false, false);
            if (!signInResult.Succeeded)
            {
                // Return an error without deleting the user
                return Ok(new { message = "Registration successful. Please log in manually." });
            }

            // Success response
            return Ok(new { message = "User registered and logged in successfully!" });
        }

        // 2. Login User
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if input is an email or username
            ApplicationUser user;
            if (model.EmailOrUsername.Contains("@"))
            {
                // It's an email
                user = await _userManager.FindByEmailAsync(model.EmailOrUsername);
            }
            else
            {
                // It's a username
                user = await _userManager.FindByNameAsync(model.EmailOrUsername);
            }

            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid email/username or password!" });
            }

            // Attempt to sign in with the retrieved username
            var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, false, false);

            if (result.Succeeded)
            {
                // Generate JWT Token
                var token = GenerateJwtToken(user);

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    Username = user.UserName,
                    Email = user.Email
                });
            }

            return Unauthorized(new { Message = "Invalid email/username or password!" });
        }

        // 3. Logout User
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "User logged out successfully!" });
        }

        // 4. Check if User is Logged In
        [HttpGet("isLoggedIn")]
        public IActionResult IsLoggedIn()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return Ok(new { isLoggedIn = true, username = User.Identity.Name });
            }

            return Ok(new { isLoggedIn = false });
        }

        // Helper: Generate JWT Token
        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id), // User ID
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique Token ID
                new Claim(ClaimTypes.Name, user.UserName) // Username
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
