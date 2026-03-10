namespace Raksha.Application.DTOs.User
{
    public class UpdateUserProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ImageKey { get; set; }
    }
}
