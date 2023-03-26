
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDDataProcessorJournalTest
{
    public class TestDbContext : IDisposable
    {
        private const string InMemoryConnectionString = "DataSource=:memory:";
        private SqliteConnection Connection { get; }
        public EdDbContext DbContext { get; }
        protected IConfiguration Configuration { get; }
        protected IServiceProvider Services { get; }

        public TestDbContext()
        {
            Connection = new SqliteConnection(InMemoryConnectionString);
            Connection.Open();
            DbContextOptions<EdDbContext> options = new DbContextOptionsBuilder<EdDbContext>()
                    .UseSqlite(Connection)
                    .Options;
            DbContext = new EdDbContext(options);
            DbContext.Database.EnsureCreated();

            Configuration = new ConfigurationBuilder()
                .Build();

            Services = new ServiceCollection()
                .AddSingleton(Configuration)
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    // builder.AddConfiguration(Configuration.GetSection("Logging"));
                })
                .BuildServiceProvider();
        }

        public void Dispose() => Connection.Close();
    }
}
