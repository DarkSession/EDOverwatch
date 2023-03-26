using Microsoft.EntityFrameworkCore.Design;

namespace EDDatabase
{
    public class EdDbContextFactory : IDesignTimeDbContextFactory<EdDbContext>
    {
        public EdDbContext CreateDbContext(string[] args)
        {
            DbContextOptions<EdDbContext> options = new DbContextOptionsBuilder<EdDbContext>()
                .UseMySql("server=localhost;user=dummy;password=dummy;database=dummy;",
                new MariaDbServerVersion(new Version(10, 3, 25)),
                options =>
                {
                    options.EnableRetryOnFailure();
                    options.CommandTimeout(60 * 10 * 1000);
                })
#if DEBUG
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine)
#endif
                .Options;
            return new EdDbContext(options);
        }
    }
}
