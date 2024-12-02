using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ITPE3200XAPI.DAL.Repositories;
using ITPE3200XAPI.Models;
using ITPE3200XAPI.DTOs.Auth;
using ITPE3200XAPI.DTOs.Setting;

namespace ITPE3200XAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IPostRepository _postRepository;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthController> logger,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            IPostRepository postRepository
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _postRepository = postRepository;
        }

        // 1. Register User
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Check if input is empty
            if (string.IsNullOrWhiteSpace(registerDto.Username) || string.IsNullOrWhiteSpace(registerDto.Email) || string.IsNullOrWhiteSpace(registerDto.Password))
            {
                _logger.LogError("Invalid model state");
                return BadRequest(new { message = "Content is required" }); // Return validation errors
            }
            
            // Check if the username or email already exists
            if (await _userManager.FindByNameAsync(registerDto.Username) != null)
            {
                _logger.LogError("Username is already taken");
                return BadRequest(new { message = "Username is already taken." });
            }

            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                _logger.LogError("Email is already taken");
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
            // Check if input is empty
            if (string.IsNullOrWhiteSpace(model.EmailOrUsername) || string.IsNullOrWhiteSpace(model.Password))
            {
                _logger.LogError("Invalid model state");
                return BadRequest(new { message = "Content is required" }); // Return validation errors
            }

            // Check if input is an email or username
            ApplicationUser? user;
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
                _logger.LogError("Invalid email/username or password");
                return Unauthorized(new { Message = "Invalid email/username or password!" });
            }

            // Attempt to sign in with the retrieved username
            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, false, false);

            if (result.Succeeded)
            {
                // Generate JWT Token
                var token = GenerateJwtToken(user);

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    Username = user.UserName!,
                    Email = user.Email!
                });
            }
            _logger.LogError("Invalid email/username or password");
            return Unauthorized(new { Message = "Invalid email/username or password!" });
        }

        // 3. Logout User
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "User logged out successfully!" });
        }
        
        // 4. Change Password 
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            // Check if input is empty
            if (string.IsNullOrWhiteSpace(model.OldPassword) || string.IsNullOrWhiteSpace(model.NewPassword) || string.IsNullOrWhiteSpace(model.Password))
            {
                _logger.LogError("Invalid model state");
                return BadRequest(new { message = "Content is required " }); // Return validation errors
            }

            // Get the user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User is not authorized");
                return Unauthorized(new { message = "User is not authorized." });
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found");
                return Unauthorized(new { message = "User not found." });
            }

            // Ensure new password and confirm new password match
            if (model.NewPassword != model.Password)
            {
                _logger.LogError("New password and confirmation password do not match");
                return BadRequest(new { message = "New password and confirmation password do not match." });
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                _logger.LogError("Password change failed");
                return BadRequest(new { message = "Password change failed.", errors = result.Errors });
            }

            return Ok(new { message = "Password changed successfully." });
        }
        
        // 5. Change Email 
        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] string email)
        {
            // Check if input is empty
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogError("Invalid model state");
                return BadRequest(new { message = "Content is required" }); // Return validation errors
            }
            
            // Get the user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User is not authorized");
                return Unauthorized(new { message = "User is not authorized." });
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found");
                return Unauthorized(new { message = "User not found." });
            }
            
            // Check if the new email is already taken
            var result = await _userManager.SetEmailAsync(user, email);
            if (!result.Succeeded)
            {
                _logger.LogError("Email change failed");
                return BadRequest(new { message = "Email change failed."});
            }

            return Ok(new { message = "Email changed successfully." });
        }
        
        //6. Delete Personal Data 
        [HttpPost("delete-personal-data")]
        public async Task<IActionResult> DeletePersonalData([FromBody] string password)
        {
            // Check if input is empty
            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogError("Invalid model state");
                return BadRequest(new { message = "Content is required" }); // Return validation errors
            }
            
            // Get the user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User is not authorized");
                return Unauthorized(new { message = "User is not authorized." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found");
                return NotFound(new { message = "User not found." });
            }

            // Verify the user's password before deletion
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid)
            {
                _logger.LogError("Incorrect password. Unable to delete account");
                return BadRequest(new { message = "Incorrect password. Unable to delete account." });
            }
            
            // Retrieve all posts authored by the user
            var userPosts = await _postRepository.GetPostsByUserAsync(userId);
            if (userPosts != null)
            {
                userPosts = userPosts.ToList(); // Create a copy to avoid modification issues
            
                foreach (var post in userPosts)
                {
                    // Retrieve and delete all images associated with the post
                    var postImages = post.Images.ToList(); // Create a copy to avoid modification issues

                    foreach (var image in postImages)
                    {
                        // Delete the image file from the server, posts gets deleted by cascade
                        DeleteImageFile(image.ImageUrl);
                    }
                }
            }

            // Delete the user's profile picture
            if (user.ProfilePictureUrl != null)
            {
                DeleteImageFile(user.ProfilePictureUrl);
            }
            
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to delete user");
                return BadRequest(new { message = "Failed to delete user."}); 
            }
            return Ok(new { message = "Your personal data has been deleted successfully." });
        }
        
        // Deletes an image file from the file system
        private void DeleteImageFile(string imageUrl)
        {
            try
            {
                // Convert the image URL to a file path
                var wwwRootPath = _webHostEnvironment.WebRootPath;
                var filePath = Path.Combine(wwwRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "[PostController][DeleteImageFile] Error deleting image file: {ImageUrl}", imageUrl);
            }
        }
        
        [HttpPost("change-number")]
        public async Task<IActionResult> ChangeNumber([FromBody] string number)
        {
            // Make sure the number is only 8 numbers, no characters
            if (number.Length != 8 || !number.All(char.IsDigit))
            {
                _logger.LogError("Invalid model state");
                return BadRequest(new { message = "Invalid phone number." }); // Return validation errors
            }
            
            // Get the user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User is not authorized");
                return Unauthorized(new { message = "User is not authorized." });
            }

            // Get the user
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found");
                return NotFound(new { message = "User not found." });
            }
            
            // Update the user's phone number
            user.PhoneNumber = number;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Number change failed");
                return BadRequest(new { message = "Number change failed."});
            }
            
            return Ok(new { message = "Number changed successfully." });
        }

        // Helper: Generate JWT Token
        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id), // User ID
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique Token ID
                new Claim(ClaimTypes.Name, user.UserName!) // Username
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
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
