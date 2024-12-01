
namespace ITPE3200XAPI.DAL.Repositories
{
    public interface IUserRepository
    {
        // Follower methods
        Task<bool> AddFollowerAsync(string followerUserId, string followedUserId);
        Task<bool> RemoveFollowerAsync(string followerUserId, string followedUserId);
        Task<bool> IsFollowingAsync(string? followerUserId, string followedUserId);
        Task<int> GetFollowerCountAsync(string userId); // How many followers a user has
        Task<int> GetFollowingCountAsync(string userId); // How many users a user is following

    }
}