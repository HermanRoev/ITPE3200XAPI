using ITPE3200XAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPE3200XAPI.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        // Follower count (how many followers a user has)
        public async Task<int> GetFollowerCountAsync(string userId)
        {
            return await _context.Followers
                .CountAsync(f => f.FollowedUserId == userId);
        }
        
        // Following count (how many users a user is following)
        public async Task<int> GetFollowingCountAsync(string userId)
        {
            return await _context.Followers
                .CountAsync(f => f.FollowerUserId == userId);
        }

        // Add follower (follow)
        public async Task<bool> AddFollowerAsync(string followerUserId, string followedUserId)
        {
            try
            {
                var follower = new Follower(followerUserId, followedUserId);
                await _context.Followers.AddAsync(follower);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while adding a follower.");
                return false;
            }
        }
        
        // Remove follower (unfollow)
        public async Task<bool> RemoveFollowerAsync(string followerUserId, string followedUserId)
        {
            try
            {
                var follower = await _context.Followers.FirstOrDefaultAsync(f => f.FollowerUserId == followerUserId && f.FollowedUserId == followedUserId);
                
                if (follower == null)
                {
                    return false;
                }
                
                _context.Followers.Remove(follower);
                await _context.SaveChangesAsync();
                    
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while removing a follower.");
                return false;
            }
        }
        
        // Check if a user is following another user 
        public async Task<bool> IsFollowingAsync(string? followerUserId, string followedUserId)
        {
            try
            {
                if (string.IsNullOrEmpty(followerUserId) || string.IsNullOrEmpty(followedUserId))
                {
                    return false;
                }
                return await _context.Followers
                    .AnyAsync(f => f.FollowerUserId == followerUserId && f.FollowedUserId == followedUserId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while checking if a user is following another user.");
                return false;
            }
        }
    }
}