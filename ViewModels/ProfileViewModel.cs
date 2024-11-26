using ITPE3200XAPI.Models;

namespace ITPE3200XAPI.ViewModels;

public class ProfileViewModel
{
    public ApplicationUser? User { get; set; } //Her ligger followers, bio, profilbilde, litt andre greier
    public List<PostViewModel>? Posts { get; set; } // Her ligger alt av post griene YIPPI
    public bool IsCurrentUserProfile { get; set; } // Om det er profilen til den innloggede brukeren
    public bool IsFollowing { get; set; } // Om den innloggede brukeren følger denne brukeren
}