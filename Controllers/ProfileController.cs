using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ITPE3200XAPI.Models;
using ITPE3200XAPI.DAL.Repositories;
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

    [Authorize]
    [HttpGet("loggedin")]
    public async Task<IActionResult> GetLoggedInProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new { message = "User not found" });
        }

        var currentUserId = _userManager.GetUserId(User);
        var posts = await _postRepository.GetPostsByUserAsync(user.Id);

        var postDtos = posts.Select(p => new PostDto
        {
            PostId = p.PostId,
            Content = p.Content,
            ProfilePicture = user.ProfilePictureUrl,
            UserName = user.UserName,
            ImageUrls = p.Images.Select(img => img.ImageUrl).ToList(),
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count
        }).ToList();

        var profileDto = new ProfileDto
        {
            Username = user.UserName,
            Bio = user.Bio,
            ProfilePictureUrl = user.ProfilePictureUrl ?? "/path/to/default-avatar.jpg",
            FollowersCount = await _userRepository.GetFollowerCountAsync(user.Id),  // Hent followers count
            FollowingCount = await _userRepository.GetFollowingCountAsync(user.Id),  // Hent following count
        };

        return Ok(new
        {
            profile = profileDto,
            posts = postDtos
        });
    }


    // GET: Full Profile Data by Username
    [HttpGet("{username}")]
    public async Task<IActionResult> GetProfile(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var currentUserId = _userManager.GetUserId(User);
        var posts = await _postRepository.GetPostsByUserAsync(user.Id);

        var postDtos = posts.Select(p => new PostDto
        {
            PostId = p.PostId,
            Content = p.Content,
            ProfilePicture = user.ProfilePictureUrl,
            UserName = user.UserName,
            ImageUrls = p.Images.Select(img => img.ImageUrl).ToList(),
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count
        }).ToList();

        // Return FollowersCount and FollowingCount, default to 0 if new user
        var followersCount = await _userRepository.GetFollowerCountAsync(user.Id);
        var followingCount = await _userRepository.GetFollowingCountAsync(user.Id);
        
        var profileDto = new ProfileDto
        {
            Username = user.UserName,
            Bio = user.Bio ?? string.Empty,
            ProfilePictureUrl = user.ProfilePictureUrl ?? "/path/to/default-avatar.jpg",
            FollowersCount = followersCount,  // Should be 0 for new users
            FollowingCount = followingCount,  // Should be 0 for new users
        };

        return Ok(new
        {
            profile = profileDto,
            posts = postDtos,
            isCurrentUserProfile = user.Id == currentUserId,
            isFollowing = currentUserId != null && await _userRepository.IsFollowingAsync(currentUserId, user.Id)
        });
    }

    // POST: Edit Profile
    [HttpPost("edit")]
    public async Task<IActionResult> EditProfile(EditProfileDto editProfileDto)
    {
        var user = await _userManager.GetUserAsync(User);
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

        var currentUserId = _userManager.GetUserId(User);
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

        var currentUserId = _userManager.GetUserId(User);
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


