using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;

namespace EDOverwatch_Web.Controllers.V1
{
    [Route("api/v1/[controller]/[action]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "ApiKeyAuthentication", Policy = "FactionUpdate")]
    public class FactionTargetController : ControllerBase
    {
        private EdDbContext DbContext { get; }
        private ActiveMqMessageProducer Producer { get; }
        public FactionTargetController(EdDbContext dbContext, ActiveMqMessageProducer producer)
        {
            DbContext = dbContext;
            Producer = producer;
        }

        [HttpPost]
        public async Task<ActionResult<FactionTargetResponse>> Create([FromBody] FactionTargetCreateDelete model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new FactionTargetResponse("Invalid request object."));
            }
            Claim? factionClaim = User.Claims.FirstOrDefault(c => c.Type == "Faction");
            if (factionClaim == null)
            {
                return Unauthorized(new FactionTargetResponse("No faction configured"));
            }

            DcohFaction? faction = await DbContext.DcohFactions
                .FirstOrDefaultAsync(f => f.Short == factionClaim.Value, cancellationToken);
            if (faction == null)
            {
                return Unauthorized(new FactionTargetResponse("Faction not found"));
            }

            StarSystem? starSystem = await DbContext.StarSystems
                .Include(s => s.ThargoidLevel)
                .FirstOrDefaultAsync(s => s.Name == model.SystemName, cancellationToken);
            if (starSystem == null)
            {
                return NotFound(new FactionTargetResponse("Star system not found"));
            }

            if (await DbContext.DcohFactionOperations
                    .Where(d => d.Faction == faction && d.Status == DcohFactionOperationStatus.Active)
                    .CountAsync(cancellationToken) >= 10)
            {
                return StatusCode((int)HttpStatusCode.NotAcceptable, new FactionTargetResponse("Limit of 10 active operations reached."));
            }

            DcohFactionOperation? dcohFactionOperation = await DbContext.DcohFactionOperations
                .FirstOrDefaultAsync(d =>
                        d.Faction == faction &&
                        d.StarSystem == starSystem &&
                        d.Status == DcohFactionOperationStatus.Active &&
                        d.Type == model.Type, cancellationToken);
            if (dcohFactionOperation == null)
            {
                dcohFactionOperation = new(0, model.Type, DcohFactionOperationStatus.Active, DateTimeOffset.Now, null)
                {
                    StarSystem = starSystem,
                    Faction = faction,
                };
                DbContext.DcohFactionOperations.Add(dcohFactionOperation);
                await DbContext.SaveChangesAsync(cancellationToken);
                return StatusCode((int)HttpStatusCode.Created, new FactionTargetResponse(true));
            }

            return Ok(new FactionTargetResponse(true));
        }

        [HttpPost]
        public async Task<ActionResult<FactionTargetResponse>> Remove([FromBody] FactionTargetCreateDelete model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new FactionTargetResponse("Invalid request object."));
            }
            Claim? factionClaim = User.Claims.FirstOrDefault(c => c.Type == "Faction");
            if (factionClaim == null)
            {
                return Unauthorized(new FactionTargetResponse("No faction configured"));
            }

            DcohFaction? faction = await DbContext.DcohFactions
                .FirstOrDefaultAsync(f => f.Short == factionClaim.Value, cancellationToken);
            if (faction == null)
            {
                return Unauthorized(new FactionTargetResponse("Faction not found"));
            }

            StarSystem? starSystem = await DbContext.StarSystems
                .Include(s => s.ThargoidLevel)
                .FirstOrDefaultAsync(s => s.Name == model.SystemName, cancellationToken);
            if (starSystem == null)
            {
                return NotFound(new FactionTargetResponse("Star system not found"));
            }

            DcohFactionOperation? dcohFactionOperation = await DbContext.DcohFactionOperations
                .FirstOrDefaultAsync(d =>
                        d.Faction == faction &&
                        d.StarSystem == starSystem &&
                        d.Status == DcohFactionOperationStatus.Active &&
                        d.Type == model.Type, cancellationToken);
            if (dcohFactionOperation == null)
            {
                return NotFound(new FactionTargetResponse("No faction operation found."));
            }

            dcohFactionOperation.Status = DcohFactionOperationStatus.Inactive;
            await DbContext.SaveChangesAsync(cancellationToken);

            return Ok(new FactionTargetResponse(true));
        }
    }

    public class FactionTargetCreateDelete
    {
        [Required]
        public string SystemName { get; set; }

        [Required]
        [EnumDataType(typeof(DcohFactionOperationType))]
        public DcohFactionOperationType Type { get; set; }

        public FactionTargetCreateDelete(string systemName, DcohFactionOperationType type)
        {
            SystemName = systemName;
            Type = type;
        }
    }

    public class FactionTargetResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public FactionTargetResponse(string message) : this(false, message)
        {
        }

        public FactionTargetResponse(bool success, string? message = null)
        {
            Success = success;
            Message = message;
        }
    }
}
