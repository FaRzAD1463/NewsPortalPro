using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace NewsPortalPro.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(Cloudinary cloudinary, IWebHostEnvironment env, ILogger<FileUploadService> logger)
        {
            _cloudinary = cloudinary;
            _env = env;
            _logger = logger;
        }

        public async Task<UploadResultDto> UploadImageAsync(IFormFile file, string folder = "news")
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
                    .FetchFormat("webp"),
                EagerTransforms = new List<Transformation>
                {
                    new Transformation().Width(400).Height(267).Crop("fill").Quality("auto")
                }
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", result.Error.Message);
                throw new InvalidOperationException($"Upload failed: {result.Error.Message}");
            }

            return new UploadResultDto
            {
                Url = result.SecureUrl.ToString(),
                ThumbnailUrl = result.EagerTransforms?.FirstOrDefault()?.SecureUrl?.ToString(),
                PublicId = result.PublicId,
                Width = result.Width,
                Height = result.Height,
                FileSizeBytes = result.Bytes
            };
        }

        public async Task<bool> DeleteAsync(string publicId)
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            return result.Result == "ok";
        }

        public async Task<UploadResultDto> UploadFromUrlAsync(string url, string folder = "news")
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(url),
                Folder = $"newsportalpro/{folder}",
                Transformation = new Transformation().Quality("auto:good").FetchFormat("webp")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return new UploadResultDto
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId,
                Width = result.Width,
                Height = result.Height
            };
        }
    }
}