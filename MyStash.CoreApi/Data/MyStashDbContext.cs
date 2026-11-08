using Microsoft.EntityFrameworkCore;

namespace MyStash.CoreApi.Data;

internal class MyStashDbContext(DbContextOptions<MyStashDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyStashDbContext).Assembly);
    }
}
