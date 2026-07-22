namespace AzureCinemaHub.Services
{
    public interface IBlobService
    {
        Task<string> UploadVideoAsync(IFormFile file);
        string GenerateSasToken(string fileName);
    }
}