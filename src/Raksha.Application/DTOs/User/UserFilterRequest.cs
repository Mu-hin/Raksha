namespace Raksha.Application.DTOs.User
{
    public class UserFilterRequest
    {
        public string? SearchTerm { get; set; }
        public int? Status { get; set; }
        public string? Role { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
