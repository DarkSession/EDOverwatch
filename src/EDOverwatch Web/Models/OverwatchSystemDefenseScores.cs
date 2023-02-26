namespace EDOverwatch_Web.Models
{
    public class OverwatchSystemDefenseScores
    {
        public List<SystemDefenseScore> Systems { get; } = new();

        public static async Task<OverwatchSystemDefenseScores> Create(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchSystemDefenseScores result = new();
            DateTimeOffset lastTick = WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, 0);
            List<StarSystem> starSystems = await dbContext.StarSystems
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.Stations!.Where(s => RelevantAssetTypes.Contains(s.Type!.Name)))
                .ThenInclude(s => s.Type)
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.StateExpires)
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Invasion)
                .ToListAsync(cancellationToken);
            if (starSystems.Any())
            {
                double maxPopSqrt = NthRoot(starSystems.Max(s => s.OriginalPopulation), 4);
                foreach (StarSystem starSystem in starSystems)
                {
                    double score = 200;
                    float dinstanceToMaelstrom = starSystem.DistanceTo(starSystem.ThargoidLevel!.Maelstrom!.StarSystem!);
                    score += (dinstanceToMaelstrom - 25d) * 5d;
                    if (starSystem.OriginalPopulation > 0)
                    {
                        score += NthRoot(starSystem.OriginalPopulation, 4) / maxPopSqrt * 100;
                    }
                    if (starSystem.ThargoidLevel?.StateExpires != null)
                    {
                        int weeksLeft = (int)Math.Ceiling((starSystem.ThargoidLevel.StateExpires.End - lastTick).TotalDays / 7);
                        score += (4 - weeksLeft) * 25;
                    }
                    if (starSystem.Stations != null && starSystem.Stations.Any())
                    {
                        int largeAssets = starSystem.Stations.Count(s => s.LandingPadLarge > 0);
                        int mediumAssets = starSystem.Stations.Count(s => s.LandingPadLarge == 0 && s.LandingPadMedium > 0);

                        if (largeAssets > 0)
                        {
                            score += Math.Sqrt(largeAssets) * 5;
                        }
                        if (mediumAssets > 0)
                        {
                            score += Math.Sqrt(mediumAssets) * 2;
                        }

                        int largeAssetsFarAway = starSystem.Stations.Count(s => s.LandingPadLarge > 0 && s.DistanceFromStarLS >= 4000);
                        int mediumAssetsFarAway = starSystem.Stations.Count(s => s.LandingPadLarge == 0 && s.LandingPadMedium > 0 && s.DistanceFromStarLS >= 4000);
                        double largeBonusBoost = 0;
                        if (largeAssets > 0)
                        {
                            largeBonusBoost = ((double)largeAssetsFarAway / (double)largeAssets) * 2.5d;
                        }
                        largeBonusBoost += 1;
                        double mediumBonusBoost = 0;
                        if (largeAssets > 0)
                        {
                            mediumBonusBoost = ((double)mediumAssetsFarAway / (double)mediumAssets) * 2.5d;
                        }
                        mediumBonusBoost += 1;

                        score += starSystem.Stations
                            .Where(s => s.State == StationState.UnderAttack)
                            .DefaultIfEmpty()
                            .Max(s =>
                            {
                                if (s != null)
                                {
                                    bool isGroundAsset = GroundAssetTypes.Contains(s.Type!.Name);
                                    double multiplier = 0;
                                    if (s.DistanceFromStarLS <= 2500)
                                    {
                                        multiplier = 1;
                                    }
                                    else if (s.DistanceFromStarLS > 2500 && s.DistanceFromStarLS < 5000)
                                    {
                                        multiplier = (5000d - (double)s.DistanceFromStarLS) / 2500;
                                    }
                                    if (s.LandingPadLarge > 0)
                                    {
                                        return (isGroundAsset ? 150 : 100) * multiplier * largeBonusBoost;
                                    }
                                    else if (s.LandingPadMedium > 0)
                                    {
                                        return 25 * multiplier * mediumBonusBoost;
                                    }
                                }
                                return 0;
                            });
                        score += starSystem.Stations
                            .Where(s => s.State == StationState.Damaged)
                            .DefaultIfEmpty()
                            .Max(s =>
                            {
                                if (s != null)
                                {
                                    bool isGroundAsset = GroundAssetTypes.Contains(s.Type!.Name);
                                    double multiplier = 0;
                                    if (s.DistanceFromStarLS <= 2000)
                                    {
                                        multiplier = 1;
                                    }
                                    else if (s.DistanceFromStarLS > 2000 && s.DistanceFromStarLS < 4000)
                                    {
                                        multiplier = (4000d - (double)s.DistanceFromStarLS) / 2000;
                                    }
                                    if (s.LandingPadLarge > 0)
                                    {
                                        return (isGroundAsset ? 100 : 150) * multiplier * largeBonusBoost;
                                    }
                                    else if (s.LandingPadMedium > 0)
                                    {
                                        return 25 * multiplier * mediumBonusBoost;
                                    }
                                }
                                return 0;
                            });
                        score += starSystem.Stations
                            .Where(s => s.State == StationState.Normal)
                            .DefaultIfEmpty()
                            .Max(s =>
                            {
                                if (s != null)
                                {
                                    bool isGroundAsset = GroundAssetTypes.Contains(s.Type!.Name);
                                    double multiplier = 0;
                                    if (s.DistanceFromStarLS <= 2000)
                                    {
                                        multiplier = 1;
                                    }
                                    else if (s.DistanceFromStarLS > 2000 && s.DistanceFromStarLS < 4000)
                                    {
                                        multiplier = (4000d - (double)s.DistanceFromStarLS) / 2000;
                                    }
                                    if (s.LandingPadLarge > 0)
                                    {
                                        double result = (isGroundAsset ? 100 : 150) * multiplier * largeBonusBoost;
                                        return result;
                                    }
                                    else if (s.LandingPadMedium > 0)
                                    {
                                        double result = 25 * multiplier * mediumBonusBoost;
                                        return result;
                                    }
                                }
                                return 0;
                            });
                    }
                    result.Systems.Add(new SystemDefenseScore(starSystem.Name, (int)Math.Round(score)));
                }
            }
            return result;
        }

        static double NthRoot(double A, int N)
        {
            return Math.Pow(A, 1.0 / N);
        }

        private static List<string> RelevantAssetTypes { get; } = new()
        {
            "Bernal",
            "Orbis",
            "Coriolis",
            "CraterOutpost",
            "MegaShip",
            "Outpost",
            "CraterPort",
            "Ocellus",
            "AsteroidBase",
        };

        private static List<string> GroundAssetTypes { get; } = new()
        {
            "CraterOutpost",
            "CraterPort",
        };
    }

    public class SystemDefenseScore
    {
        public string Name { get; }
        public int DefenseScore { get; }

        public SystemDefenseScore(string name, int defenseScore)
        {
            Name = name;
            DefenseScore = defenseScore;
        }
    }
}
