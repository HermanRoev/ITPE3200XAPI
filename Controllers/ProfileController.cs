using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ITPE3200XAPI.Models;
using ITPE3200XAPI.DAL.Repositories;
using ITPE3200XAPI.DTOs.Auth;
using ITPE3200XAPI.DTOs;
using Microsoft.AspNetCore.Identity;

namespace ITPE3200XAPI.Controllers;

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

    // GET: Profile Data by Username
    [Authorize]
    [HttpGet("{username}")]
    public async Task<IActionResult> GetProfile(string username)
    {
        // Hent bruker basert på username
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            _logger.LogWarning("[ProfileController][GetProfile] User '{Username}' not found.", username);
            return NotFound(new { message = "User not found" });
        }

        // Hent followers og following count
        var followersCount = await _userRepository.GetFollowerCountAsync(user.Id);
        var followingCount = await _userRepository.GetFollowingCountAsync(user.Id);

        // Bygg ProfileDto
        var profileDto = new ProfileDto
        {
            Username = user.UserName,
            Bio = user.Bio,
            ProfilePictureUrl = user.ProfilePictureUrl ?? "/path/to/default-avatar.jpg",
            FollowersCount = followersCount,
            FollowingCount = followingCount
        };

        return Ok(profileDto);
    }

    // GET: Posts by User
    [Authorize]
    [HttpGet("{username}/posts")]
    public async Task<IActionResult> GetPostsByUser(string username)
    {
        // Hent bruker basert på username
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            _logger.LogWarning("[ProfileController][GetPostsByUser] User '{Username}' not found.", username);
            return NotFound(new { message = "User not found" });
        }

        // Hent alle innlegg av brukeren
        var posts = await _postRepository.GetPostsByUserAsync(user.Id);

        // Bygg en liste med PostDto
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

        return Ok(postDtos);
    }

    // POST: Follow a User
    [Authorize]
    [HttpPost("follow")]
    public async Task<IActionResult> Follow([FromBody] string username)
    {
        var userToFollow = await _userManager.FindByNameAsync(username);
        if (userToFollow == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var currentUserId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var result = await _userRepository.AddFollowerAsync(currentUserId, userToFollow.Id);
        if (!result)
        {
            return StatusCode(500, new { message = "Could not follow user" });
        }

        return Ok(new { message = "User followed successfully" });
    }

    // POST: Unfollow a User
    [Authorize]
    [HttpPost("unfollow")]
    public async Task<IActionResult> Unfollow([FromBody] string username)
    {
        var userToUnfollow = await _userManager.FindByNameAsync(username);
        if (userToUnfollow == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var currentUserId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var result = await _userRepository.RemoveFollowerAsync(currentUserId, userToUnfollow.Id);
        if (!result)
        {
            return StatusCode(500, new { message = "Could not unfollow user" });
        }

        return Ok(new { message = "User unfollowed successfully" });
    }
}
