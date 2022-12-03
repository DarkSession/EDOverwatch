using Microsoft.EntityFrameworkCore.Design;

namespace EDDatabase
{
    public class EdDbContextFactory : IDesignTimeDbContextFactory<EdDbContext>
    {
        public EdDbContext CreateDbContext(string[] args)
        {
            return new EdDbContext("server=localhost;user=dummy;password=dummy;database=dummy;");
        }
    }
}
