using Raksha.Application.Models;

namespace Raksha.Application.Interfaces.Services
{
    public interface IFileService
    {
        Task<Result> SaveProfilePictureAsync(Guid userId, Stream fileStream, string fileName, CancellationToken ct = default);
        Task<(Stream? FileStream, string? ContentType)?> GetProfilePictureAsync(Guid userId, CancellationToken ct = default);
        Task<Result> DeleteProfilePictureAsync(Guid userId, CancellationToken ct = default);
    }
}
