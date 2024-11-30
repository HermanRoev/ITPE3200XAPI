namespace ITPE3200XAPI.DTOs.Profile
{
    public class ProfileDto
    {
        // Brukernavn til profilen
        public string Username { get; set; } = string.Empty;

        // Brukerens biografi
        public string? Bio { get; set; }

        // URL til profilbildet
        public string ProfilePictureUrl { get; set; } = string.Empty;

        // Antall følgere
        public int FollowersCount { get; set; }

        // Antall brukere denne personen følger
        public int FollowingCount { get; set; }
    }
}
