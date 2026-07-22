using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using AzureCinemaHub.Data;
using AzureCinemaHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AzureCinemaHub.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobContainerClient _blobContainerClient;

        public MoviesController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;

            // Беремо рядок підключення з appsettings або Environment Variables в Azure
            var connectionString = configuration.GetConnectionString("AzureBlobStorage")
                                  ?? configuration["AzureBlobStorage"];

            // Переконайся, що "movies" — це реальна назва твого контейнера у Blob Storage
            _blobContainerClient = new BlobContainerClient(connectionString, "movies");
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            var movies = await _context.Movies.OrderByDescending(m => m.CreatedAt).ToListAsync();
            return View(movies);
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();

            if (!string.IsNullOrEmpty(movie.BlobFileName))
            {
                var blobClient = _blobContainerClient.GetBlobClient(movie.BlobFileName);

                if (await blobClient.ExistsAsync())
                {
                    // Генеруємо SAS-токен на 2 години
                    var sasBuilder = new BlobSasBuilder
                    {
                        BlobContainerName = _blobContainerClient.Name,
                        BlobName = movie.BlobFileName,
                        Resource = "b",
                        ExpiresOn = DateTimeOffset.UtcNow.AddHours(2)
                    };
                    sasBuilder.SetPermissions(BlobSasPermissions.Read);

                    var sasUri = blobClient.GenerateSasUri(sasBuilder);
                    ViewBag.SasUrl = sasUri.ToString();
                }
            }

            return View(movie);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie movie, IFormFile videoFile)
        {
            if (ModelState.IsValid)
            {
                if (videoFile != null && videoFile.Length > 0)
                {
                    // Створюємо контейнер, якщо його ще не існує
                    await _blobContainerClient.CreateIfNotExistsAsync();

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                    var blobClient = _blobContainerClient.GetBlobClient(fileName);

                    using (var stream = videoFile.OpenReadStream())
                    {
                        await blobClient.UploadAsync(stream, true);
                    }

                    movie.BlobFileName = fileName;
                    movie.CreatedAt = DateTime.UtcNow;

                    _context.Add(movie);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                // 1. Видаляємо файл з Azure Blob Storage
                if (!string.IsNullOrEmpty(movie.BlobFileName))
                {
                    var blobClient = _blobContainerClient.GetBlobClient(movie.BlobFileName);
                    await blobClient.DeleteIfExistsAsync();
                }

                // 2. Видаляємо запис з Azure SQL
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}