using Microsoft.EntityFrameworkCore;
using FetchFlow.Database.Service.Entities;

namespace FetchFlow.Database.Service.Context
{
    public class ApiContext : DbContext
    {
        public ApiContext(DbContextOptions<ApiContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
    }
}
