using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ITPE3200XAPI.Models;
using ITPE3200XAPI.DAL.Repositories;
using ITPE3200XAPI.DTOs.Auth;
using ITPE3200XAPI.DTOs.Profile;
using ITPE3200XAPI.DTOs.Post;
using Microsoft.AspNetCore.Identity;

namespace ITPE3200XAPI.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly IPostRepository _postRepository;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IUserRepository userRepository,
        IPostRepository postRepository,
        ILogger<ProfileController> logger
    )
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _postRepository = postRepository;
        _logger = logger;
    }
    
    [HttpGet]
    [Route("basic")]
    public async Task<IActionResult> GetBasicProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogError("Failed to find userId");
            return Unauthorized(new { message = "User not found." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogError("Failed to find user with ID '{UserId}'.", userId);
            return Unauthorized(new { message = "User not found." });
        }

        var profileDto = new SideMenuProfileDto
        {
            Username = user.UserName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Email = user.Email,
        };

        return Ok(profileDto);
    }

    // GET: Full Profile Data by Username
    [HttpGet("{username?}")]
    public async Task<IActionResult> GetProfile(string? username)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(username))
        {
            var checkUser = await _userManager.FindByIdAsync(currentUserId);
            if (checkUser == null)
            {
                _logger.LogError("Failed to find user with ID '{UserId}'.", currentUserId);
                return NotFound(new { message = "User not found." });
            }
            
            username = checkUser.UserName;
            if (username == null)
            {
                _logger.LogError("Failed to find username for user with ID '{UserId}'.", currentUserId);
                return NotFound(new { message = "User not found." });
            }
        }
        
        var user = await _userManager.FindByNameAsync(username);
        
        if (user == null)
        {
            _logger.LogError("Failed to find user with username '{Username}'.", username);
            return NotFound(new { message = "User not found." });
        }
        
        var dynamicPosts = await _postRepository.GetPostsByUserAsync(user.Id);

        if (dynamicPosts == null)
        {
            _logger.LogError("Failed to find posts for user with ID '{UserId}'.", user.Id);
            return NotFound("No posts found");
        }

        // Convert the IEnumerable<Post> to a List<Post> to avoid multiple enumeration
        dynamicPosts = dynamicPosts.ToList();

        // Construct the list of postDtos to pass to the frontend
        var postDtos = dynamicPosts.Select(p => new PostDto
        {
            PostId = p.PostId,
            Content = p.Content,
            ImageUrls = p.Images.Select(img => img.ImageUrl).ToList(),
            UserName = p.User.UserName,
            ProfilePicture = p.User.ProfilePictureUrl,
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
            IsSavedByCurrentUser = p.SavedPosts.Any(sp => sp.UserId == currentUserId),
            IsOwnedByCurrentUser = p.UserId == currentUserId,
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            Comments = p.Comments
                .OrderBy(c => c.CreatedAt)
                .Take(20) // Limit number of comments returned
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    UserName = c.User.UserName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    TimeSincePosted = CalculateTimeSincePosted(c.CreatedAt),
                    IsCreatedByCurrentUser = c.UserId == currentUserId
                })
                .ToList()
        }).ToList();

        // Return FollowersCount and FollowingCount, default to 0 if new user
        var followersCount = await _userRepository.GetFollowerCountAsync(user.Id);
        var followingCount = await _userRepository.GetFollowingCountAsync(user.Id);
        
        var profileDto = new ProfileDto
        {
            Username = user.UserName!,
            Bio = user.Bio ?? string.Empty,
            ProfilePictureUrl = user.ProfilePictureUrl,
            FollowersCount = followersCount,  // Should be 0 for new users
            FollowingCount = followingCount,  // Should be 0 for new users
            IsCurrentUserProfile = user.Id == currentUserId,
            IsFollowing = await _userRepository.IsFollowingAsync(currentUserId, user.Id)
        };

        return Ok(new
        {
            profile = profileDto,
            posts = postDtos,
        });
    }

    // Calculates the time since a post or comment was created
    private string CalculateTimeSincePosted(DateTime createdAt)
    {
        var currentTime = DateTime.UtcNow;

        // Check if the createdAt timestamp is in the future
        if (createdAt > currentTime)
        {
            // Log a warning if createdAt is in the future
            _logger.LogWarning("[HomeController][CalculateTimeSincePosted] CreatedAt timestamp is in the future: {CreatedAt}", createdAt);
            // Adjust createdAt to current time to prevent negative time spans
            createdAt = currentTime;
        }

        var timeSpan = currentTime - createdAt;

        // Determine the appropriate time format
        if (timeSpan.TotalMinutes < 60)
        {
            return $"{(int)timeSpan.TotalMinutes} m ago";
        }
        else if (timeSpan.TotalHours < 24)
        {
            return $"{(int)timeSpan.TotalHours} h ago";
        }
        else
        {
            return $"{(int)timeSpan.TotalDays} d ago";
        }
    }
    
    // POST: Edit Profile
    [HttpPost("edit")]
    public async Task<IActionResult> EditProfile(EditProfileDto editProfileDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized(new { message = "User not found." });
        }
        
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        user.Bio = editProfileDto.Bio;
        user.ProfilePictureUrl = editProfileDto.ProfilePictureUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to update user profile for user '{UserId}'.", user.Id);
            return StatusCode(500, new { message = "Could not update profile." });
        }

        return Ok(new { message = "Profile updated successfully." });
    }

    // POST: Follow a User
    [HttpPost("follow")]
    public async Task<IActionResult> Follow([FromBody] string username)
    {
        var userToFollow = await _userManager.FindByNameAsync(username);
        if (userToFollow == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "You are not logged in." });
        }

        var result = await _userRepository.AddFollowerAsync(currentUserId, userToFollow.Id);
        if (!result)
        {
            return StatusCode(500, new { message = "Could not follow user." });
        }

        return Ok(new { message = "User followed successfully." });
    }

    // POST: Unfollow a User
    [HttpPost("unfollow")]
    public async Task<IActionResult> Unfollow([FromBody] string username)
    {
        var userToUnfollow = await _userManager.FindByNameAsync(username);
        if (userToUnfollow == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "You are not logged in." });
        }

        var result = await _userRepository.RemoveFollowerAsync(currentUserId, userToUnfollow.Id);
        if (!result)
        {
            return StatusCode(500, new { message = "Could not unfollow user." });
        }

        return Ok(new { message = "User unfollowed successfully." });
    }
}