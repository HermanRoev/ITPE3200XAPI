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
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok(new { Message = "User registered successfully!" });
            }

            return BadRequest(result.Errors);
        }

        // 2. Login User
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return Unauthorized(new { Message = "Invalid credentials" });
                }

                // Generate JWT Token
                var token = GenerateJwtToken(user);

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    Username = user.UserName,
                    Email = user.Email
                });
            }

            return Unauthorized(new { Message = "Invalid email or password!" });
        }

        // 3. Logout User
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { Message = "User logged out successfully!" });
        }

        // 4. Check if User is Logged In
        [HttpGet("isLoggedIn")]
        public IActionResult IsLoggedIn()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return Ok(new { IsLoggedIn = true, Username = User.Identity.Name });
            }

            return Ok(new { IsLoggedIn = false });
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
