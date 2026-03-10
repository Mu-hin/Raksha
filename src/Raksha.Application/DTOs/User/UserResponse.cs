using Raksha.Domain.Common;

namespace Raksha.Application.DTOs.User
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ImageKey { get; set; }
        public List<string> Roles { get; set; } = new();
        public EntityStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
