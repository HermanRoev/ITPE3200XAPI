using ITPE3200XAPI.Models;

namespace ITPE3200XAPI.DAL.Repositories
{
    public interface IUserRepository
    {
        // Follower methods
        Task<bool> AddFollowerAsync(string followerUserId, string followedUserId);
        Task<bool> RemoveFollowerAsync(string followerUserId, string followedUserId);
        Task<bool> IsFollowingAsync(string followerUserId, string followedUserId);
        Task<int> GetFollowerCountAsync(string userId);
        Task<int> GetFollowingCountAsync(string userId);

    }
}