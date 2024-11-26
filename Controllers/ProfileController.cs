using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ITPE3200XAPI.Models;
using ITPE3200XAPI.DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using ITPE3200XAPI.ViewModels;

namespace ITPE3200XAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly IPostRepository _postRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<ProfileController> _logger;

    // Constructor injecting dependencies
    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IUserRepository userRepository,
        IPostRepository postRepository,
        IWebHostEnvironment webHostEnvironment,
        ILogger<ProfileController> logger
    )
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _postRepository = postRepository;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    // GET: Profile
    [AllowAnonymous]
    [HttpGet]
    [Route("/profile/{username?}")]
    public async Task<IActionResult> Profile(string? username)
    {
        if (string.IsNullOrEmpty(username))
        {
            // If username is not provided, get the current user's username to show their profile
            username = _userManager.GetUserName(User);
            if (string.IsNullOrEmpty(username))
            {
                // There is no profile to show
                return NotFound("User not found");
            }
        }

        // Retrieve the user by username
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            _logger.LogWarning("[ProfileController][Profile] User '{Username}' not found.", username);
            return NotFound("User not found");
        }

        var currentUserId = _userManager.GetUserId(User);

        // Retrieve posts by the user
        var posts = await _postRepository.GetPostsByUserAsync(user.Id);

        // Construct the list of PostViewModels
        var postViewModels = posts.Select(p => new PostViewModel
        {
            PostId = p.PostId,
            Content = p.Content,
            Images = p.Images.ToList(),
            UserName = p.User.UserName,
            ProfilePicture = p.User.ProfilePictureUrl,
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
            IsSavedByCurrentUser = p.SavedPosts.Any(sp => sp.UserId == currentUserId),
            IsOwnedByCurrentUser = p.UserId == currentUserId,
            HomeFeed = false,
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            Comments = p.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentViewModel
                {
                    IsCreatedByCurrentUser = c.UserId == currentUserId,
                    CommentId = c.CommentId,
                    UserName = c.User.UserName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    TimeSincePosted = CalculateTimeSincePosted(c.CreatedAt)
                })
                .ToList()
        }).ToList();

        // Determine if the current user is following the profile user
        var isFollowing = false;
        if (User.Identity!.IsAuthenticated && currentUserId != user.Id)
        {
            isFollowing = await _userRepository.IsFollowingAsync(currentUserId!, user.Id);
        }

        // Prepare the ProfileViewModel
        var profile = new ProfileViewModel
        {
            User = user,
            Posts = postViewModels,
            IsCurrentUserProfile = user.Id == currentUserId,
            IsFollowing = isFollowing
        };

        return View(profile);
    }

    // Calculates the time since a post or comment was created
    private string CalculateTimeSincePosted(DateTime createdAt)
    {
        var currentTime = DateTime.UtcNow;

        // Check if the createdAt timestamp is in the future
        if (createdAt > currentTime)
        {
            _logger.LogWarning("[ProfileController][CalculateTimeSincePosted] CreatedAt timestamp is in the future: {CreatedAt}", createdAt);
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

    // GET: EditProfile
    [HttpGet]
    [Route("/profile/edit")]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var model = new EditProfileViewModel
        {
            Bio = user.Bio,
            ProfilePictureUrl = user.ProfilePictureUrl
        };

        return View(model);
    }

    // POST: EditProfile
    [HttpPost]
    [Route("/profile/edit")]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("", "Invalid model.");
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        // Handle Profile Picture Upload
        if (model.ImageFile != null)
        {
            // Validate the image file
            if (!IsImageFile(model.ImageFile))
            {
                ModelState.AddModelError("ImageFile", "The file is not a valid image.");
                return View(model);
            }

            // Generate a unique file name
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
            var uploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profile_pictures");
            var filePath = Path.Combine(uploads, fileName);

            // Ensure the uploads directory exists
            if (!Directory.Exists(uploads))
            {
                Directory.CreateDirectory(uploads);
            }

            // Save the image to the server
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(fileStream);
            }

            // Delete the old profile picture if it exists and is not the default
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                DeleteImageFile(user.ProfilePictureUrl);
            }

            // Update the user's ProfilePictureUrl
            user.ProfilePictureUrl = $"/uploads/profile_pictures/{fileName}";
        }

        // Update other user properties
        user.Bio = model.Bio;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("[ProfileController][Edit POST] Failed to update user profile for '{UserId}'.", user.Id);
            ModelState.AddModelError("", "Could not update profile.");
            return View(model);
        }

        return RedirectToAction("Profile", new { username = user.UserName });
    }

    // Deletes an image file from the file system
    private void DeleteImageFile(string imageUrl)
    {
        try
        {
            var wwwRootPath = _webHostEnvironment.WebRootPath;
            var filePath = Path.Combine(wwwRootPath,
                imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProfileController][DeleteImageFile] Error deleting image file: {ImageUrl}", imageUrl);
        }
    }

    // Checks if a file is a valid image file based on its extension
    private bool IsImageFile(IFormFile file)
    {
        var permittedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return !string.IsNullOrEmpty(extension) && permittedExtensions.Contains(extension);
    }

    // POST: Follow a user
    [HttpPost]
    [Route("/profile/follow")]
    public async Task<IActionResult> Follow(string username)
    {
        var userToFollow = await _userManager.FindByNameAsync(username);
        if (userToFollow == null)
        {
            _logger.LogWarning("[ProfileController][Follow] User '{Username}' not found.", username);
            return NotFound("User not found");
        }

        var currentUserId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var result = await _userRepository.AddFollowerAsync(currentUserId, userToFollow.Id);
        
        if (!result)
        {
            _logger.LogError("[ProfileController][Follow] Failed to follow user '{Username}'.", username);
            ModelState.AddModelError("", "Could not follow user.");
            return RedirectToAction("Profile", new { username });
        }
        
        return RedirectToAction("Profile", new { username });
    }

    // POST: Unfollow a user
    [HttpPost]
    [Route("/profile/unfollow")]
    public async Task<IActionResult> Unfollow(string username)
    {
        var userToUnfollow = await _userManager.FindByNameAsync(username);
        
        if (userToUnfollow == null)
        {
            _logger.LogWarning("[ProfileController][Unfollow] User '{Username}' not found.", username);
            return NotFound("User not found");
        }

        var currentUserId = _userManager.GetUserId(User);
        
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var result = await _userRepository.RemoveFollowerAsync(currentUserId, userToUnfollow.Id);
        
        if (!result)
        {
            _logger.LogError("[ProfileController][Unfollow] Failed to unfollow user '{Username}'.", username);
            ModelState.AddModelError("", "Could not unfollow user.");
            return RedirectToAction("Profile", new { username });
        }
        
        return RedirectToAction("Profile", new { username });
    }
}