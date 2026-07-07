using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NewsPortalPro.DTOs;
using NewsPortalPro.Interfaces;

namespace NewsPortalPro.Services
{
        public class FileUploadService : IFileUploadService
        {
        private readonly Cloudinary? _cloudinary;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileUploadService> _logger;

        // ── Allowed file signatures (magic bytes) ─────────────────
        // Verifies the actual file bytes match the claimed extension.
        // Prevents .exe renamed to .jpg from being accepted.
        private static readonly Dictionary<string, byte[][]> MagicBytes =
            new()
            {
                {
                    ".jpg", new byte[][]
                    {
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xDB }
                    }
                },
                {
                    ".jpeg", new byte[][]
                    {
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                        new byte[] { 0xFF, 0xD8, 0xFF, 0xDB }
                    }
                },
                {
                    ".png", new byte[][]
                    {
                        new byte[] { 0x89, 0x50, 0x4E, 0x47 }
                    }
                },
                {
                    ".gif", new byte[][]
                    {
                        new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 },
                        new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }
                    }
                },
                {
                    ".webp", new byte[][]
                    {
                        new byte[] { 0x52, 0x49, 0x46, 0x46 }
                    }
                }
            };

            // ── Allowed MIME types ─────────────────────────────────────
            private static readonly HashSet<string> AllowedMimeTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/gif",
                "image/webp"
            };

            // ── Allowed extensions ─────────────────────────────────────
            private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp"
            };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        private const int MagicBytesReadSize = 8;

        public FileUploadService(
            Cloudinary? cloudinary,
            IWebHostEnvironment env,
            ILogger<FileUploadService> logger)
        {
            _cloudinary = cloudinary;
            _env = env;
            _logger = logger;
        }

            public async Task<UploadResultDto> UploadImageAsync(
            IFormFile file,
            string folder = "news")
            {
            // ── Step 1: Basic null/size check ─────────────────────

            if (file == null || file.Length == 0)
                throw new ArgumentException(
                    "No file provided or file is empty.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException(
                    $"File size {file.Length / 1024 / 1024}MB exceeds " +
                    $"the maximum allowed size of " +
                    $"{MaxFileSizeBytes / 1024 / 1024}MB.");

            // ── Step 2: Extension validation ──────────────────────

            var extension = Path.GetExtension(file.FileName)
                                .ToLowerInvariant()
                                .Trim();

            if (string.IsNullOrEmpty(extension) ||
                !AllowedExtensions.Contains(extension))
                throw new ArgumentException(
                    $"File extension '{extension}' is not allowed. " +
                    $"Allowed: {string.Join(", ", AllowedExtensions)}");

            // ── Step 3: MIME type validation ──────────────────────

            var mimeType = file.ContentType?.ToLowerInvariant().Trim()
                        ?? string.Empty;

            if (!AllowedMimeTypes.Contains(mimeType))
                throw new ArgumentException(
                    $"Content type '{mimeType}' is not allowed.");

            // ── Step 4: Magic byte validation ─────────────────────
            // Read the first bytes of the file to verify it truly is
            // what it claims to be — this is the critical security check.

            await using var stream = file.OpenReadStream();

            var headerBytes = new byte[MagicBytesReadSize];
            var bytesRead = await stream.ReadAsync(
                headerBytes.AsMemory(0, MagicBytesReadSize));

            if (bytesRead < 4)
                throw new ArgumentException(
                    "File is too small to be a valid image.");

            if (!MagicBytes.TryGetValue(extension, out var signatures))
                throw new ArgumentException(
                    $"No signature defined for extension '{extension}'.");

            var signatureValid = signatures.Any(sig =>
                headerBytes.Take(sig.Length).SequenceEqual(sig));

            if (!signatureValid)
            {
                _logger.LogWarning(
                    "File upload rejected — magic byte mismatch. " +
                    "Extension: {Extension}, MIME: {Mime}, " +
                    "Bytes: {Bytes}",
                    extension,
                    mimeType,
                    BitConverter.ToString(
                        headerBytes.Take(8).ToArray()));

                throw new ArgumentException(
                    "File content does not match its claimed extension. " +
                    "Upload rejected.");
            }

            // Reset stream position after reading header bytes
            stream.Position = 0;

            // ── Step 5: Upload via Cloudinary or local storage ────
            // Use Cloudinary if configured, otherwise save locally.
            if (_cloudinary != null)
                return await UploadToCloudinaryAsync(
                    stream, file, folder, extension);
            else
                return await SaveLocallyAsync(
                    stream, file, folder, extension);
            }

            // ── Cloudinary upload ──────────────────────────────────────
            private async Task<UploadResultDto> UploadToCloudinaryAsync(
            Stream stream,
            IFormFile file,
            string folder,
            string extension)
            {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(
                    $"{Guid.NewGuid():N}{extension}", stream),
                Folder = $"newsportalpro/{folder}",
                Transformation = new Transformation()
                    .Quality("auto:good")
                    .FetchFormat("webp")
            };

            var result = await _cloudinary!.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                _logger.LogError(
                    "Cloudinary upload error: {Error}",
                    result.Error.Message);
                throw new InvalidOperationException(
                    $"Upload failed: {result.Error.Message}");
            }

            // Build thumbnail URL via Cloudinary transformation
            var thumbnailUrl = result.SecureUrl != null
                ? result.SecureUrl.ToString()
                    .Replace("/upload/", "/upload/w_400,h_267,c_fill/")
                : null;

            _logger.LogInformation(
                "Cloudinary upload successful. PublicId: {PublicId}",
                result.PublicId);

            return new UploadResultDto
            {
                Url = result.SecureUrl?.ToString()
                                ?? string.Empty,
                ThumbnailUrl = thumbnailUrl,
                PublicId = result.PublicId,
                Width = result.Width,
                Height = result.Height,
                FileSizeBytes = result.Bytes
            };
            }

        // ── Local file storage fallback ────────────────────────────
            private async Task<UploadResultDto> SaveLocallyAsync(
            Stream stream,
            IFormFile file,
            string folder,
            string extension)
            {
            // Sanitize folder name — prevent path traversal

            var safeFolder = Path.GetFileName(folder.Trim());
            if (string.IsNullOrEmpty(safeFolder))
                safeFolder = "general";

            // Always use a random filename — never the original

            var safeFileName = $"{Guid.NewGuid():N}{extension}";
            var uploadFolder = Path.Combine(
                _env.WebRootPath, "uploads", safeFolder);

            Directory.CreateDirectory(uploadFolder);

            // Verify resolved path stays inside wwwroot/uploads

            var fullPath = Path.GetFullPath(
                Path.Combine(uploadFolder, safeFileName));
            var allowedRootPath = Path.GetFullPath(
                Path.Combine(_env.WebRootPath, "uploads"));

            if (!fullPath.StartsWith(
                    allowedRootPath,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Path traversal attempt detected.");

            await using var fileStream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await stream.CopyToAsync(fileStream);

            var url = $"/uploads/{safeFolder}/{safeFileName}";

            _logger.LogInformation(
                "Local upload successful. " +
                "File: {FileName}, Size: {Size} bytes",
                safeFileName, file.Length);

            return new UploadResultDto
            {
                Url = url,
                ThumbnailUrl = url,
                PublicId = safeFileName,
                FileSizeBytes = file.Length
            };
        }

            public async Task<bool> DeleteAsync(string publicId)
            {
            if (string.IsNullOrWhiteSpace(publicId))
                return false;

            // ── Cloudinary delete ──────────────────────────────────

            if (_cloudinary != null)
            {
                try
                {
                    var result = await _cloudinary.DestroyAsync(
                        new DeletionParams(publicId));

                    var success = result.Result == "ok";
                    if (!success)
                        _logger.LogWarning(
                            "Cloudinary delete failed for: {PublicId}",
                            publicId);
                    return success;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Cloudinary delete error for: {PublicId}",
                        publicId);
                    return false;
                }
            }

            // ── Local delete ───────────────────────────────────────

            var safePublicId = Path.GetFileName(publicId);
            if (string.IsNullOrEmpty(safePublicId)) return false;

            try
            {
                var uploadsRoot = Path.Combine(
                    _env.WebRootPath, "uploads");

                var files = Directory.GetFiles(
                    uploadsRoot, safePublicId,
                    SearchOption.AllDirectories);

                foreach (var f in files)
                {
                    var resolvedPath = Path.GetFullPath(f);
                    var allowedRootPath = Path.GetFullPath(uploadsRoot);

                    if (!resolvedPath.StartsWith(
                            allowedRootPath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "Path traversal in delete: {Path}",
                            resolvedPath);
                        continue;
                    }

                    File.Delete(f);
                    _logger.LogInformation(
                        "Local file deleted: {File}", safePublicId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Local delete failed: {PublicId}", safePublicId);
                return false;
            }
        }

            public async Task<UploadResultDto> UploadFromUrlAsync(
            string url,
            string folder = "news")
            {
            if (_cloudinary != null)
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
                    Url = result.SecureUrl?.ToString()
                                    ?? string.Empty,
                    ThumbnailUrl = result.SecureUrl?.ToString()
                                    .Replace("/upload/",
                                        "/upload/w_400,h_267,c_fill/"),
                    PublicId = result.PublicId,
                    Width = result.Width,
                    Height = result.Height,
                    FileSizeBytes = result.Bytes
                };
            }

            // No Cloudinary — return URL as-is
            return new UploadResultDto
            {
                Url = url,
                ThumbnailUrl = url,
                PublicId = Guid.NewGuid().ToString("N")
            };
            }
    }
}