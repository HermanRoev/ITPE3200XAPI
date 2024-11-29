using ITPE3200XAPI.DAL.Repositories;
using ITPE3200XAPI.Models;
using ITPE3200XAPI.DTOs.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ITPE3200XAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly IPostRepository _postRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<PostController> _logger;

        public PostController(
            ILogger<PostController> logger,
            IPostRepository postRepository,
            IWebHostEnvironment webHostEnvironment
        )
        {
            _logger = logger;
            _postRepository = postRepository;
            _webHostEnvironment = webHostEnvironment;
        }
    
        // TODO: Implement EditPost method
        
        // POST: api/Post/CreatePost
        [HttpPost("CreatePost")]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto createPostDto)
        {
            if (string.IsNullOrWhiteSpace(createPostDto.Content))
            {
                return BadRequest(new { message = "Content is required." });
            }

            if (createPostDto.ImageFiles == null || !createPostDto.ImageFiles.Any())
            {
                return BadRequest(new { message = "At least one image is required." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = new Post(userId, createPostDto.Content);

            // Handle image files
            foreach (var imageFile in createPostDto.ImageFiles)
            {
                if (imageFile.Length > 0)
                {
                    if (!IsImageFile(imageFile))
                    {
                        return BadRequest(new { message = "One or more files are not valid images." });
                    }

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                    var uploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    var filePath = Path.Combine(uploads, fileName);

                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    var imageUrl = $"/uploads/{fileName}";
                    var postImage = new PostImage(post.PostId, imageUrl);

                    post.Images.Add(postImage);
                }
            }

            var result = await _postRepository.AddPostAsync(post);
            if (!result)
            {
                _logger.LogError("Failed to create post for UserId: {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while creating the post." });
            }

            var postDto = await GetPostDtoById(post.PostId);
            return CreatedAtAction(nameof(GetPostById), new { postId = post.PostId }, postDto);
        }

        // POST: api/Post/ToggleLike/{postId}
        [HttpPost("ToggleLike/{postId}")]
        public async Task<IActionResult> ToggleLike(string postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
            {
                return NotFound(new { message = "Post not found." });
            }

            bool isLiked = post.Likes.Any(l => l.UserId == userId);
            if (!isLiked)
            {
                await _postRepository.AddLikeAsync(postId, userId);
            }
            else
            {
                await _postRepository.RemoveLikeAsync(postId, userId);
            }

            var postDto = await GetPostDtoById(postId);
            return Ok(postDto);
        }

        // POST: api/Post/ToggleSave/{postId}
        [HttpPost("ToggleSave/{postId}")]
        public async Task<IActionResult> ToggleSave(string postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
            {
                return NotFound(new { message = "Post not found." });
            }

            bool isSaved = post.SavedPosts.Any(sp => sp.UserId == userId);
            if (!isSaved)
            {
                await _postRepository.AddSavedPostAsync(postId, userId);
            }
            else
            {
                await _postRepository.RemoveSavedPostAsync(postId, userId);
            }

            var postDto = await GetPostDtoById(postId);
            return Ok(postDto);
        }

        // POST: api/Post/AddComment
        [HttpPost("AddComment")]
        public async Task<IActionResult> AddComment([FromBody] AddCommentDto addCommentDto)
        {
            if (string.IsNullOrWhiteSpace(addCommentDto.Content))
            {
                return BadRequest(new { message = "Comment content cannot be empty." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = await _postRepository.GetPostByIdAsync(addCommentDto.PostId);
            if (post == null)
            {
                return NotFound(new { message = "Post not found." });
            }

            var comment = new Comment(addCommentDto.PostId, userId, addCommentDto.Content);

            var result = await _postRepository.AddCommentAsync(comment);
            if (!result)
            {
                _logger.LogError("Failed to add comment to PostId: {PostId} by UserId: {UserId}", addCommentDto.PostId, userId);
                return StatusCode(500, new { message = "An error occurred while adding the comment." });
            }

            var postDto = await GetPostDtoById(addCommentDto.PostId);
            return Ok(postDto);
        }
        
        [HttpPost("EditComment")]
        public async Task<IActionResult> EditComment([FromBody] EditCommentDto editCommentDto)
        {
            if (string.IsNullOrWhiteSpace(editCommentDto.Content))
            {
                return BadRequest(new { message = "Comment content cannot be empty." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = await _postRepository.GetPostByIdAsync(editCommentDto.PostId);
            if (post == null)
            {
                return NotFound(new { message = "Post not found." });
            }
            
            var result = await _postRepository.EditCommentAsync(editCommentDto.CommentId, userId, editCommentDto.Content);
            if (!result)
            {
                _logger.LogError("Failed to edit comment {CommentId} by UserId: {UserId}", editCommentDto.CommentId, userId);
                return StatusCode(500, new { message = "An error occurred while editing the comment." });
            }

            var postDto = await GetPostDtoById(editCommentDto.PostId);
            return Ok(postDto);
        }
        
        [AllowAnonymous]
        [HttpPost("DeleteComment")]
        public async Task<IActionResult> DeleteComment([FromQuery] string postId, [FromQuery] string commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine("UserId: " + userId);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
            {
                return NotFound(new { message = "Post not found." });
            }
            
            var result = await _postRepository.DeleteCommentAsync(commentId, userId);
            if (!result)
            {
                _logger.LogError("Failed to delete comment {CommentId} from PostId: {PostId} by UserId: {UserId}", commentId, postId, userId);
                return StatusCode(500, new { message = "An error occurred while deleting the comment." });
            }

            var postDto = await GetPostDtoById(postId);
            return Ok(postDto);
        }
        
        // TODO: Implement Image delete from file system
        [HttpPost("DeletePost/{postId}")]
        public async Task<IActionResult> DeletePost(string postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
            {
                return NotFound(new { message = "Post not found." });
            }

            var result = await _postRepository.DeletePostAsync(postId, userId);
            if (!result)
            {
                _logger.LogError("Failed to delete PostId: {PostId} by UserId: {UserId}", postId, userId);
                return StatusCode(500, new { message = "An error occurred while deleting the post." });
            }

            return Ok(new { message = "Post deleted successfully." });
        }

        // GET: api/Post/GetPostById/{postId}
        [HttpGet("GetPostById/{postId}")]
        public async Task<IActionResult> GetPostById(string postId)
        {
            var postDto = await GetPostDtoById(postId);
            if (postDto == null)
            {
                return NotFound(new { message = "Post not found." });
            }

            return Ok(postDto);
        }

        // Helper Methods

        private bool IsImageFile(IFormFile file)
        {
            var permittedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            return !string.IsNullOrEmpty(extension) && permittedExtensions.Contains(extension);
        }

        private async Task<PostDto?> GetPostDtoById(string postId)
        {
            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
            {
                return null;
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var postDto = new PostDto
            {
                PostId = post.PostId,
                Content = post.Content,
                ImageUrls = post.Images.Select(img => img.ImageUrl).ToList(),
                UserName = post.User.UserName,
                ProfilePicture = post.User.ProfilePictureUrl,
                IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId),
                IsSavedByCurrentUser = post.SavedPosts.Any(sp => sp.UserId == currentUserId),
                IsOwnedByCurrentUser = post.UserId == currentUserId,
                LikeCount = post.Likes.Count,
                CommentCount = post.Comments.Count,
                Comments = post.Comments
                    .OrderBy(c => c.CreatedAt)
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
            };

            return postDto;
        }

        private string CalculateTimeSincePosted(DateTime createdAt)
        {
            var currentTime = DateTime.UtcNow;
            var timeSpan = currentTime - createdAt;

            if (timeSpan.TotalMinutes < 60)
            {
                return $"{(int)timeSpan.TotalMinutes}m ago";
            }
            else if (timeSpan.TotalHours < 24)
            {
                return $"{(int)timeSpan.TotalHours}h ago";
            }
            else
            {
                return $"{(int)timeSpan.TotalDays}d ago";
            }
        }
    }
}