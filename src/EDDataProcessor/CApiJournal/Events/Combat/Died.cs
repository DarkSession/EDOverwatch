using System.Runtime.Serialization;

namespace EDDataProcessor.CApiJournal.Events.Combat
{
    internal class Died : JournalEvent
    {
        [JsonProperty(Required = Required.Default)]
        public string? KillerShip { get; set; }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(KillerShip) && !Enum.TryParse<Ship>(KillerShip, true, out _))
            {
                bool isThargoidKill = KillerShip switch
                {
                    "scout_q" => true,
                    "scout_nq" => true,
                    "scout" => true,
                    "thargonswarm" => true,
                    "thargon" => true,
                    _ => false
                };
                if (isThargoidKill)
                {
                    await AddOrUpdateWarEffort(journalParameters, WarEffortType.KillGeneric, 1, WarEffortSide.Thargoids, dbContext, cancellationToken);
                }
                else
                {
                    await DeferEvent(journalParameters, dbContext, cancellationToken);
                }
            }
        }

        enum Ship
        {
            SideWinder = 128049249,
            Eagle = 128049255,
            Hauler = 128049261,
            Adder = 128049267,
            [EnumMember(Value = "Viper")]
            ViperMkIII = 128049273,
            CobraMkIII = 128049279,
            Type6 = 128049285,
            Dolphin = 128049291,
            Type7 = 128049297,
            [EnumMember(Value = "Asp")]
            AspExplorer = 128049303,
            Vulture = 128049309,
            [EnumMember(Value = "Empire_Trader")]
            ImperialClipper = 128049315,
            [EnumMember(Value = "Federation_Dropship")]
            FederalDropship = 128049321,
            Orca = 128049327,
            Type9 = 128049333,
            Python = 128049339,
            BelugaLiner = 128049345,
            FerDeLance = 128049351,
            Anaconda = 128049363,
            [EnumMember(Value = "Federation_Corvette")]
            FederalCorvette = 128049369,
            [EnumMember(Value = "Cutter")]
            ImperialCutter = 128049375,
            [EnumMember(Value = "DiamondBack")]
            DiamondbackScout = 128671217,
            [EnumMember(Value = "Empire_Courier")]
            ImperialCourier = 128671223,
            [EnumMember(Value = "DiamondBackXL")]
            DiamondbackExplorer = 128671831,
            [EnumMember(Value = "Empire_Eagle")]
            ImperialEagle = 128672138,
            [EnumMember(Value = "Federation_Dropship_MkII")]
            FederalAssaultShip = 128672145,
            [EnumMember(Value = "Federation_Gunship")]
            FederalGunship = 128672152,
            [EnumMember(Value = "Viper_MkIV")]
            ViperMkIV = 128672255,
            CobraMkIV = 128672262,
            [EnumMember(Value = "Independant_Trader")]
            Keelback = 128672269,
            [EnumMember(Value = "Asp_Scout")]
            AspScout = 128672276,
            [EnumMember(Value = "Type9_Military")]
            Type10 = 128785619,
            [EnumMember(Value = "Krait_MkII")]
            KraitMkII = 128816567,
            [EnumMember(Value = "TypeX")]
            AllianceChieftain = 128816574,
            [EnumMember(Value = "TypeX_2")]
            AllianceCrusader = 128816581,
            [EnumMember(Value = "TypeX_3")]
            AllianceChallenger = 128816588,
            [EnumMember(Value = "Krait_Light")]
            KraitPhantom = 128839281,
            Mamba = 128915979,

            NotShipsAbove = 900000000,

            [EnumMember(Value = "vulture_taxi")]
            VultureTaxi = 999999800,

            [EnumMember(Value = "TestBuggy")]
            SRV = 999999900,

            [EnumMember(Value = "independent_fighter")]
            IndepdenentFighter = 999999990,
            [EnumMember(Value = "empire_fighter")]
            EmpireFighter = 999999991,
            [EnumMember(Value = "federation_fighter")]
            FederationFighter = 999999992,
            [EnumMember(Value = "gdn_hybrid_fighter_v1")]
            GuardianFighterV1 = 999999993,
            [EnumMember(Value = "gdn_hybrid_fighter_v2")]
            GuardianFighterV2 = 999999994,
            [EnumMember(Value = "gdn_hybrid_fighter_v3")]
            GuardianFighterV3 = 999999995,
        }
    }
}
