using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(
            Cloudinary cloudinary,
            ILogger<FileUploadService> logger)
        {
            _cloudinary = cloudinary;
            _logger = logger;
        }

        public async Task<UploadResultDto> UploadImageAsync(
            IFormFile file, string folder = "news")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = $"newsportalpro/{folder}",
                Transformation = new Transformation()
                    .Quality("auto:good")
                    .FetchFormat("webp")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                _logger.LogError(
                    "Cloudinary upload error: {Error}", result.Error.Message);
                throw new InvalidOperationException(
                    $"Upload failed: {result.Error.Message}");
            }

            // Generate thumbnail URL manually via Cloudinary transformation URL
            var thumbnailUrl = result.SecureUrl != null
                ? result.SecureUrl.ToString()
                    .Replace("/upload/", "/upload/w_400,h_267,c_fill/")
                : null;

            return new UploadResultDto
            {
                Url = result.SecureUrl?.ToString() ?? string.Empty,
                ThumbnailUrl = thumbnailUrl,
                PublicId = result.PublicId,
                Width = result.Width,
                Height = result.Height,
                FileSizeBytes = result.Bytes
            };
        }

        public async Task<bool> DeleteAsync(string publicId)
        {
            var result = await _cloudinary
                .DestroyAsync(new DeletionParams(publicId));
            return result.Result == "ok";
        }

        public async Task<UploadResultDto> UploadFromUrlAsync(
            string url, string folder = "news")
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(url),
                Folder = $"newsportalpro/{folder}",
                Transformation = new Transformation()
                    .Quality("auto:good")
                    .FetchFormat("webp")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            return new UploadResultDto
            {
                Url = result.SecureUrl?.ToString() ?? string.Empty,
                PublicId = result.PublicId,
                Width = result.Width,
                Height = result.Height,
                FileSizeBytes = result.Bytes
            };
        }
    }
}