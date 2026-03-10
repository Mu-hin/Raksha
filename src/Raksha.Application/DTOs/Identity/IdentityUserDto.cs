namespace Raksha.Application.DTOs.Identity
{
    public class IdentityUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? ImageKey { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
