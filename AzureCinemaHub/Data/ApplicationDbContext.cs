using AzureCinemaHub.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AzureCinemaHub.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Movie> Movies { get; set; } = null!;
    }
}