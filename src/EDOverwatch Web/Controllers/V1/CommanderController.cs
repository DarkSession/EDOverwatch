using EDUtils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EDOverwatch_Web.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]/[action]")]
    public class CommanderController : ControllerBase
    {
        private UserManager<ApplicationUser> UserManager { get; }
        private EdDbContext DbContext { get; }

        public CommanderController(
            UserManager<ApplicationUser> userManager,
            EdDbContext dbContext
        )
        {
            UserManager = userManager;
            DbContext = dbContext;
        }

        public async Task<List<CommanderWarEffort>> GetWarEfforts(CancellationToken cancellationToken)
        {
            ApplicationUser? user = await UserManager.GetUserAsync(User);
            if (user == null) throw new Exception("User is null");
            List<WarEffort> warEfforts = await DbContext.WarEfforts
                .AsNoTracking()
                .Include(w => w.StarSystem)
                .Where(w => w.CommanderId == user.CommanderId)
                .ToListAsync(cancellationToken);
            return warEfforts.Select(w => new CommanderWarEffort(w)).ToList();
        }
    }

    public class CommanderWarEffort
    {
        public DateOnly Date { get; set; }
        public string Type { get; set; }
        public string SystemName { get; set; }
        public long Amount { get; set; }

        public CommanderWarEffort(WarEffort warEffort)
        {
            Date = warEffort.Date;
            Type = warEffort.Type.GetEnumMemberValue();
            SystemName = warEffort.StarSystem?.Name ?? "Unknown";
            Amount = warEffort.Amount;
        }
    }
}
