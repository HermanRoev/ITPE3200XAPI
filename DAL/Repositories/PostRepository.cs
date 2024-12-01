using ITPE3200XAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPE3200XAPI.DAL.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostRepository> _logger;

        public PostRepository(ApplicationDbContext context, ILogger<PostRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Post methods
        public async Task<Post?> GetPostByIdAsync(string postId)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.Images)
                    .Include(p => p.User)
                    .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                    .Include(p => p.Likes)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PostId == postId);

                if (post == null)
                {
                    return null;
                }

                return post;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while retrieving a post by ID.");
                return null;
            }

        }
        
        // SavedPost methods
        // Returns saved posts for a user, based on the user id
        public async Task<IEnumerable<Post>?> GetSavedPostsByUserIdAsync(string userId)
        {
            try
            {
                var savedPosts = await _context.SavedPosts
                    .Where(sp => sp.UserId == userId)
                    .OrderByDescending(sp => sp.CreatedAt)
                    .Include(sp => sp.Post)
                    .ThenInclude(p => p.User)
                    .Include(sp => sp.Post.Images)
                    .Include(sp => sp.Post.Comments)
                    .ThenInclude(c => c.User)
                    .Include(sp => sp.Post.Likes)
                    .ThenInclude(l => l.User)
                    .AsNoTracking()
                    .ToListAsync();

                var posts = savedPosts.Select(sp => sp.Post);

                return posts;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while retrieving saved posts by user ID.");
                return null;
            }
        }


        public async Task<IEnumerable<Post>?> GetAllPostsAsync()
        {
            try
            {
                return await _context.Posts
                                .Include(p => p.User)
                                .Include(p => p.Images)
                                .Include(p => p.Comments)
                                .ThenInclude(c => c.User)
                                .Include(p => p.Likes)
                                .ThenInclude(l => l.User)
                                .Include(p => p.SavedPosts)
                                .OrderByDescending(p => p.CreatedAt)
                                .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while retrieving all posts.");
                return null;
            }
            
        }
        
        // Add a post to the database
        public async Task<bool> AddPostAsync(Post post)
        {
            try
            {
                await _context.Posts.AddAsync(post);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while adding a post.");
                return false;
            }
            
        }
        
        // Update a post in the database
        public async Task<bool> UpdatePostAsync(Post post, List<PostImage> imagesToDelete, List<PostImage> imagesToAdd)
        {
            try
            {
                if (imagesToDelete.Count > 0)
                {
                    foreach (var image in imagesToDelete)
                    {
                       _context.PostImages.Remove(image);
                    }
                }
                if (imagesToAdd.Count > 0)
                {
                    foreach (var image in imagesToAdd)
                    {
                        await _context.PostImages.AddAsync(image);
                    }
                }
                
                _context.Posts.Update(post);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating a post.");
                return false;
            }

        }
        
        // Delete a post from the database
        public async Task<bool> DeletePostAsync(string postId, string userId)
        {
            try
            {
                var post = await _context.Posts.FindAsync(postId);
                if (post!.UserId != userId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to delete this post.");
                }
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while deleting a post.");
                return false;
            }
        }

        // Comment methods
        public async Task<bool> AddCommentAsync(Comment comment)
        {
            try
            {
                await _context.Comments.AddAsync(comment);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while adding a comment.");
                return false;
            }
        }
        
        // Delete a comment from the database
        public async Task<bool> DeleteCommentAsync(string commentId, string userId)
        {
            try
            {
                var comment = await _context.Comments.FindAsync(commentId);
                
                if (comment != null)
                {
                    if (comment.UserId != userId)
                    {
                        throw new UnauthorizedAccessException("You are not authorized to delete this comment.");
                    }
                    _context.Comments.Remove(comment);
                    await _context.SaveChangesAsync();
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while deleting a comment.");
                return false;
            }

        }
        
        // Edit a comment in the database
        public async Task<bool> EditCommentAsync(string commentId, string userId, string content)
        {
            try
            {
                var comment = await _context.Comments.FindAsync(commentId);

                if (comment != null)
                {
                    if (comment.UserId != userId)
                    {
                        throw new UnauthorizedAccessException("You are not authorized to edit this comment.");
                    }

                    comment.Content = content;
                    await _context.SaveChangesAsync();
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while editing a comment.");
                return false;
            }
        }

        // Like methods
        public async Task<bool> AddLikeAsync(string postId, string userId)
        {
            try
            {
                var like = new Like(postId, userId);
                await _context.Likes.AddAsync(like);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while adding a like.");
                return false;
            }
        }
        
        // Remove a like from the database
        public async Task<bool> RemoveLikeAsync(string postId, string userId)
        {
            try
            {
                var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
                if (like != null)
                {
                    _context.Likes.Remove(like);
                    await _context.SaveChangesAsync();
                }
                
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while removing a like.");
                return false;
            }
            
        }
        
        // Save post methods
        public async Task<bool> AddSavedPostAsync(String postId, string userId)
        {
            try
            { 
                var savedPost = new SavedPost(postId, userId);
                await _context.SavedPosts.AddAsync(savedPost);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while saving a post.");
                return false;
            }
        }
        
        //Removed saved post method
        public async Task<bool> RemoveSavedPostAsync(String postId, string userId)
        {
            try
            {
                var savedPost = await _context.SavedPosts
                .FirstOrDefaultAsync(sp => sp.PostId == postId && sp.UserId == userId);
                if (savedPost != null)
                {
                    _context.SavedPosts.Remove(savedPost);
                    await _context.SaveChangesAsync();
                }
                
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while removing a saved post.");
                return false;
            }
        }
        
        // Get posts by user
        public async Task<IEnumerable<Post>?> GetPostsByUserAsync(string userId)
        {
            try
            {
                var post = await _context.Posts
                    .Where(p => p.UserId == userId)
                    .Include(p => p.User)
                    .Include(p => p.Images)
                    .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                    .Include(p => p.Likes)
                    .ThenInclude(l => l.User)
                    .Include(p => p.SavedPosts)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return post;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while getting all posts.");
                return null;
            }
        }
    }
}