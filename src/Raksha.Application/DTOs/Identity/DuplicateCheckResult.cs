namespace Raksha.Application.DTOs.Identity
{
    public class DuplicateCheckResult
    {
        public bool IsDuplicateEmail { get; set; }
        public bool IsDuplicateUserName { get; set; }
    }
}
