namespace Raksha.Application.Models
{
    public class FileSettings
    {
        public const string SectionName = "FileSettings";
        public string ProfilePictureStoragePath { get; set; } = "uploads/profile-pictures";
        public long MaxFileSizeBytes { get; set; } = 2097152; // 2MB
        public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png"];
    }
}
