using Raksha.Application.Models;

namespace Raksha.Application.Interfaces.Services
{
    public interface IFileService
    {
        Task<Result> SaveProfilePictureAsync(Guid userId, Stream fileStream, string fileName);
        Task<(Stream? FileStream, string? ContentType)?> GetProfilePictureAsync(Guid userId);
        Task<Result> DeleteProfilePictureAsync(Guid userId);
    }
}
