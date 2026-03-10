using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raksha.Application.Interfaces.Services;
using Raksha.Application.Models;

namespace Raksha.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly FileSettings _fileSettings;
        private readonly ILogger<FileService> _logger;

        private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

        public FileService(IOptions<FileSettings> fileSettings, ILogger<FileService> logger)
        {
            _fileSettings = fileSettings.Value;
            _logger = logger;
        }

        public async Task<Result> SaveProfilePictureAsync(Guid userId, Stream fileStream, string fileName)
        {
            if (fileStream.Length == 0)
                return Result.Failure("File is empty.");

            if (fileStream.Length > _fileSettings.MaxFileSizeBytes)
                return Result.Failure($"File size exceeds the maximum allowed size of {_fileSettings.MaxFileSizeBytes / 1024 / 1024}MB.");

            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_fileSettings.AllowedExtensions.Contains(extension))
                return Result.Failure($"File type not allowed. Allowed types: {string.Join(", ", _fileSettings.AllowedExtensions)}");

            var storagePath = Path.GetFullPath(_fileSettings.ProfilePictureStoragePath);
            Directory.CreateDirectory(storagePath);

            // Delete existing files for this user (different extensions)
            foreach (var ext in _fileSettings.AllowedExtensions)
            {
                var existingFile = Path.Combine(storagePath, $"{userId}{ext}");
                if (File.Exists(existingFile))
                    File.Delete(existingFile);
            }

            var filePath = Path.Combine(storagePath, $"{userId}{extension}");
            using var outputStream = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(outputStream);

            _logger.LogInformation("Profile picture saved for user {UserId}: {FileName}", userId, $"{userId}{extension}");

            return Result.Success($"{userId}{extension}");
        }

        public Task<(Stream? FileStream, string? ContentType)?> GetProfilePictureAsync(Guid userId)
        {
            var storagePath = Path.GetFullPath(_fileSettings.ProfilePictureStoragePath);

            foreach (var ext in _fileSettings.AllowedExtensions)
            {
                var filePath = Path.Combine(storagePath, $"{userId}{ext}");
                if (File.Exists(filePath))
                {
                    var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var contentType = ContentTypes.GetValueOrDefault(ext, "application/octet-stream");
                    return Task.FromResult<(Stream? FileStream, string? ContentType)?>((stream, contentType));
                }
            }

            return Task.FromResult<(Stream? FileStream, string? ContentType)?>(null);
        }

        public Task<Result> DeleteProfilePictureAsync(Guid userId)
        {
            var storagePath = Path.GetFullPath(_fileSettings.ProfilePictureStoragePath);

            foreach (var ext in _fileSettings.AllowedExtensions)
            {
                var filePath = Path.Combine(storagePath, $"{userId}{ext}");
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            return Task.FromResult(Result.Success("Profile picture deleted."));
        }
    }
}
