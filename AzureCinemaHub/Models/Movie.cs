using System.ComponentModel.DataAnnotations;

namespace AzureCinemaHub.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Назва обов'язкова")]
        [Display(Name = "Назва фільму")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Опис")]
        public string Description { get; set; } = string.Empty;

        public string BlobFileName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}