using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace MeraKharcha.Infrastructure
{
    public interface IFileStorageService
    {
        Task<(bool Success, string Message, string? FilePath)> SaveReceiptAsync(IFormFile file);
        void DeleteFile(string relativePath);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<(bool Success, string Message, string? FilePath)> SaveReceiptAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "No file uploaded.", null);
            }

            if (file.Length > MaxFileSize)
            {
                return (false, "File size exceeds maximum limit of 5MB.", null);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return (false, $"Unsupported file type. Allowed formats: {string.Join(", ", _allowedExtensions)}", null);
            }

            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var physicalPath = Path.Combine(uploadFolder, uniqueFileName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/receipts/{uniqueFileName}";
            return (true, "File uploaded successfully.", relativePath);
        }

        public void DeleteFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            var cleanPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_env.WebRootPath, cleanPath);
            if (File.Exists(physicalPath))
            {
                try
                {
                    File.Delete(physicalPath);
                }
                catch
                {
                    // Ignore deletion error for cleanup
                }
            }
        }
    }
}
