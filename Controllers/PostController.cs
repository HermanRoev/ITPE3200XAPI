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
                        _logger.LogError("One or more files are not valid images.");
                        return BadRequest(new { message = "One or more files are not valid images." });
                    }
                    
                    // Extract the file extension and ensure it's in lowercase
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    // Generate a unique filename with the extension
                    var fileName = $"{Guid.NewGuid()}{extension}";
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
                _logger.LogError("User not found.");
                return Unauthorized();
            }

            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
            {
                _logger.LogError("Post not found: {PostId}", postId);
                return NotFound(new { message = "Post not found." });
            }

            bool isLiked = post.Likes.Any(l => l.UserId == userId);
            if (!isLiked)
            {
                var result = await _postRepository.AddLikeAsync(postId, userId);
                if (!result)
                {
                    _logger.LogError("Failed to add like to PostId: {PostId} by UserId: {UserId}", postId, userId);
                    return StatusCode(500, new { message = "An error occurred while adding the like." });
                }
            }
            else
            {
                var result = await _postRepository.RemoveLikeAsync(postId, userId);
                if (!result)
                {
                    _logger.LogError("Failed to remove like from PostId: {PostId} by UserId: {UserId}", postId, userId);
                    return StatusCode(500, new { message = "An error occurred while removing the like." });
                }
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
                var result = await _postRepository.AddSavedPostAsync(postId, userId);
                if (!result)
                {
                    _logger.LogError("Failed to add saved post to PostId: {PostId} by UserId: {UserId}", postId, userId);
                    return StatusCode(500, new { message = "An error occurred while adding the saved post." });
                }
            }
            else
            {
                var result = await _postRepository.RemoveSavedPostAsync(postId, userId);
                if (!result)
                {
                    _logger.LogError("Failed to remove saved post from PostId: {PostId} by UserId: {UserId}", postId, userId);
                    return StatusCode(500, new { message = "An error occurred while removing the saved post." });
                }
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
                _logger.LogError("Comment content cannot be empty.");
                return BadRequest(new { message = "Comment content cannot be empty." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User not found.");
                return Unauthorized();
            }

            var post = await _postRepository.GetPostByIdAsync(editCommentDto.PostId);
            if (post == null)
            {
                _logger.LogError("Post not found: {PostId}", editCommentDto.PostId);
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
                _logger.LogError("User not found.");
                return Unauthorized();
            }

            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
            {
                _logger.LogError("Post not found: {PostId}", postId);
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
        
        [HttpPost("DeletePost/{postId}")]
        public async Task<IActionResult> DeletePost(string postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User not found.");
                return Unauthorized();
            }

            var post = await _postRepository.GetPostByIdAsync(postId);
            if (post == null)
            {
                _logger.LogError("Post not found: {PostId}", postId);
                return NotFound(new { message = "Post not found." });
            }
            
            // Delete image files from the file system
            foreach (var image in post.Images)
            {
                DeleteImageFile(image.ImageUrl);
            }

            var result = await _postRepository.DeletePostAsync(postId, userId);
            if (!result)
            {
                _logger.LogError("Failed to delete PostId: {PostId} by UserId: {UserId}", postId, userId);
                return StatusCode(500, new { message = "An error occurred while deleting the post." });
            }

            return Ok(new { message = "Post deleted successfully." });
        }
        
        // POST: api/Post/EditPost    
        [HttpPost("editpost")]
        [Authorize]
        public async Task<IActionResult> EditPost([FromForm] EditPostDto dto)
        {
            // Validate the DTO
            if (string.IsNullOrEmpty(dto.Content))
            {
                _logger.LogError("[PostController][EditPost] Content is required.");
                return BadRequest(new { message = "Content is required." });
            }

            if ((dto.ImageFiles == null || !dto.ImageFiles.Any()) &&
                (dto.ExistingImageUrls == null || !dto.ExistingImageUrls.Any()))
            {
                _logger.LogError("[PostController][EditPost] At least one image is required.");
                return BadRequest(new { message = "At least one image is required." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("[PostController][EditPost] User not found.");
                return Unauthorized(new { message = "User not found." });
            }
            
            if(dto.PostId == null)
            {
                _logger.LogError("[PostController][EditPost] PostId is required.");
                return BadRequest(new { message = "PostId is required." });
            }

            var postToUpdate = await _postRepository.GetPostByIdAsync(dto.PostId);
            
            if (postToUpdate == null)
            {
                _logger.LogError("[PostController][EditPost] Post not found: {PostId}", dto.PostId);
                return NotFound(new { message = "Post not found." });
            }

            // Check ownership
            if (postToUpdate.UserId != userId)
            {
                _logger.LogError("[PostController][EditPost] User does not own the post.");
                return Forbid();
            }

            // Update the content
            postToUpdate.Content = dto.Content;

            // Prepare lists for images to delete and add
            List<PostImage> imagesToDelete;
            var imagesToAdd = new List<PostImage>();

            // Identify images to delete (not included in ExistingImageUrls)
            if (dto.ExistingImageUrls != null && dto.ExistingImageUrls.Any())
            {
                imagesToDelete = postToUpdate.Images
                    .Where(image => !dto.ExistingImageUrls.Contains(image.ImageUrl))
                    .ToList();
            }
            else
            {
                // If no ExistingImageUrls are provided, delete all current images
                imagesToDelete = postToUpdate.Images.ToList();
            }

            // Remove files from the server for images marked for deletion
            foreach (var image in imagesToDelete)
            {
                DeleteImageFile(image.ImageUrl);
                postToUpdate.Images.Remove(image);
            }

            if (dto.ImageFiles != null && dto.ImageFiles.Any())
            {
                // Add new images
                foreach (var imageFile in dto.ImageFiles)
                {
                    if (imageFile.Length > 0)
                    {
                        // Validate the image file
                        if (!IsImageFile(imageFile))
                        {
                            _logger.LogError("[PostController][EditPost] One or more files are not valid images.");
                            return BadRequest(new { message = "One or more files are not valid images." });
                        }
    
                        // Extract the file extension and ensure it's in lowercase
                        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                        // Generate a unique filename with the extension
                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var uploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                        var filePath = Path.Combine(uploads, fileName);
    
                        // Ensure the uploads directory exists
                        if (!Directory.Exists(uploads))
                        {
                            Directory.CreateDirectory(uploads);
                        }
    
                        // Save the image to the server
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
    
                        // Create PostImage entity and add to the list
                        var imageEntity = new PostImage(postToUpdate.PostId, $"/uploads/{fileName}");
                        imagesToAdd.Add(imageEntity);
                    }
                }
            }
            
            // Add new images to the post
            foreach (var image in imagesToAdd)
            {
                postToUpdate.Images.Add(image);
            }

            // Save changes to the database
            var result = await _postRepository.UpdatePostAsync(postToUpdate, imagesToDelete, imagesToAdd);

            if (!result)
            {
                _logger.LogError("[PostController][EditPost] Error updating post in database.");
                return StatusCode(500, new { message = "An error occurred while updating the post." });
            }

            return Ok(new {message = "Post updated successfully."});
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