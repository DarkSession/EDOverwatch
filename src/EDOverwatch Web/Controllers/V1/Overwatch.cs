﻿using EDOverwatch_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]/[action]")]
    [AllowAnonymous]
    [EnableCors("ApiCORS")]
    public class Overwatch : ControllerBase
    {
        private EdDbContext DbContext { get; }
        private IMemoryCache MemoryCache { get; }
        public Overwatch(EdDbContext dbContext, IMemoryCache memoryCache)
        {
            DbContext = dbContext;
            MemoryCache = memoryCache;
        }

        private const string OverviewCacheKey = "OverwatchOverview";
        [HttpGet]
        public async Task<OverwatchOverview> Overview(CancellationToken cancellationToken)
        {
            if (!MemoryCache.TryGetValue(OverviewCacheKey, out OverwatchOverview? result))
            {
                result = await OverwatchOverview.LoadOverwatchOverview(DbContext, cancellationToken);
                MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
                MemoryCache.Set(OverviewCacheKey, result, cacheEntryOptions);
            }
            return result!;
        }

        private const string SystemsCacheKey = "OverwatchSystems";
        [HttpGet]
        public async Task<OverwatchStarSystems> Systems(CancellationToken cancellationToken)
        {
            if (!MemoryCache.TryGetValue(SystemsCacheKey, out OverwatchStarSystems? result))
            {
                result = await OverwatchStarSystems.Create(DbContext, cancellationToken);
                MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
                MemoryCache.Set(SystemsCacheKey, result, cacheEntryOptions);
            }
            return result!;
        }

        private const string SystemsCycleCacheKey = "OverwatchSystemsCycle";
        [HttpGet("{cycle}")]
        public async Task<OverwatchStarSystemsHistorical> Systems(DateOnly cycle, CancellationToken cancellationToken)
        {
            string cacheKey = SystemsCycleCacheKey + cycle.ToString();
            if (!MemoryCache.TryGetValue(cacheKey, out OverwatchStarSystemsHistorical? result))
            {
                result = await OverwatchStarSystemsHistorical.Create(cycle, DbContext, cancellationToken);
                MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
                MemoryCache.Set(cacheKey, result, cacheEntryOptions);
            }
            return result!;
        }

        private const string SystemCacheKey = "OverwatchSystem";
        [HttpGet("{systemAddress}")]
        public async Task<OverwatchStarSystemDetail?> System(long systemAddress, CancellationToken cancellationToken)
        {
            string cacheKey = SystemCacheKey + systemAddress.ToString();
            if (!MemoryCache.TryGetValue(cacheKey, out OverwatchStarSystemDetail? result))
            {
                result = await OverwatchStarSystemDetail.Create(systemAddress, DbContext, cancellationToken);
                MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                MemoryCache.Set(cacheKey, result, cacheEntryOptions);
            }
            return result!;
        }

        private const string MaelstromsCacheKey = "OverwatchMaelstroms";
        [HttpGet]
        public async Task<OverwatchMaelstroms> Maelstroms(CancellationToken cancellationToken)
        {
            if (!MemoryCache.TryGetValue(MaelstromsCacheKey, out OverwatchMaelstroms? result))
            {
                result = await OverwatchMaelstroms.Create(DbContext, cancellationToken);
                MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
                MemoryCache.Set(MaelstromsCacheKey, result, cacheEntryOptions);
            }
            return result!;
        }

        private const string ThargoidCyclesCacheKey = "OverwatchThargoidCycles";
        [HttpGet]
        public async Task<List<OverwatchThargoidCycle>> ThargoidCycles(CancellationToken cancellationToken)
        {
            if (!MemoryCache.TryGetValue(ThargoidCyclesCacheKey, out List<OverwatchThargoidCycle>? result))
            {
                result = await OverwatchThargoidCycle.GetThargoidCycles(DbContext, cancellationToken);
                MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
                MemoryCache.Set(ThargoidCyclesCacheKey, result, cacheEntryOptions);
            }
            return result!;
        }
    }
}
