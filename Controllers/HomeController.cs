using System.Security.Claims;
using ITPE3200XAPI.DAL.Repositories;
using Microsoft.AspNetCore.Mvc;
using ITPE3200XAPI.Models;
using Microsoft.AspNetCore.Identity;
using ITPE3200XAPI.DTOs.Post;

namespace ITPE3200XAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class HomeController : Controller
{
    private readonly IPostRepository _postRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IPostRepository postRepository,
        UserManager<ApplicationUser> userManager,
        ILogger<HomeController> logger
        )
    {
        _postRepository = postRepository;
        _userManager = userManager;
        _logger = logger;
    }

    // Returns the home view, accessible to all users even if they are not logged in
    [HttpGet]
    [Route("/index")]
    public async Task<IActionResult> Index()
    {
        // Retrieve all dynamic posts from the repository
        var dynamicPosts = await _postRepository.GetAllPostsAsync();

        if (dynamicPosts == null)
        {
            // Error in the repository, return an empty view
            return NotFound("No posts found");
        }

        // Convert the IEnumerable<Post> to a List<Post> to avoid multiple enumeration
        dynamicPosts = dynamicPosts.ToList();

        // Get the current user's ID (can be null if the user is not logged in)
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Construct the list of postDtos to pass to the frontend
        var postDtos = dynamicPosts.Select(p => new PostDto
        {
            PostId = p.PostId,
            Content = p.Content,
            ImageUrls = p.Images?.Select(img => img.ImageUrl).ToList(),
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
        
        // Simulate slow connection
        //await Task.Delay(5000);
        
        return Ok(postDtos);
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
}