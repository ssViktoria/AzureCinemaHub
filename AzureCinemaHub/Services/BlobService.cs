using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace AzureCinemaHub.Services
{
    public class BlobService : IBlobService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BlobService> _logger;

        public BlobService(IConfiguration configuration, ILogger<BlobService> logger)
        {
            _logger = logger;
            try
            {
                // Отримуємо рядок підключення з налаштувань
                var connectionString = configuration.GetConnectionString("AzureBlobStorage");
                var blobServiceClient = new BlobServiceClient(connectionString);
                // Отримуємо посилання на контейнер "movies"
                _containerClient = blobServiceClient.GetBlobContainerClient("movies");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критична помилка при підключенні до Azure Blob Storage");
                throw;
            }
        }

        public async Task<string> UploadVideoAsync(IFormFile file)
        {
            try
            {
                // Генеруємо унікальне ім'я, щоб уникнути конфліктів файлів з однаковою назвою
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var blobClient = _containerClient.GetBlobClient(uniqueFileName);

                // Відкриваємо потік і завантажуємо у хмару
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                return uniqueFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка завантаження файлу {FileName} в Azure Blob Storage", file.FileName);
                throw; // Перенаправляємо помилку для логування вищих рівнів
            }
        }

        public string GenerateSasToken(string fileName)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(fileName);

                // Будуємо параметри токена
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerClient.Name,
                    BlobName = fileName,
                    Resource = "b", // "b" означає Blob
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(120) // Працює точно 120 хвилин
                };

                // Вказуємо права: ТІЛЬКИ ЧИТАННЯ
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                // Генеруємо URI з токеном на кінці
                Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при згенерованості SAS токена для файлу {FileName}", fileName);
                throw;
            }
        }
    }
}