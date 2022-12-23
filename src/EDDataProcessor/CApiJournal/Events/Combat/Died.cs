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
                    "scout_hq" => true,
                    "scout_nq" => true,
                    "scout_q" => true,
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
            [EnumMember(Value = "viper")]
            ViperMkIII = 128049273,
            CobraMkIII = 128049279,
            Type6 = 128049285,
            Dolphin = 128049291,
            Type7 = 128049297,
            [EnumMember(Value = "asp")]
            AspExplorer = 128049303,
            Vulture = 128049309,
            [EnumMember(Value = "empire_trader")]
            ImperialClipper = 128049315,
            [EnumMember(Value = "federation_dropship")]
            FederalDropship = 128049321,
            Orca = 128049327,
            Type9 = 128049333,
            Python = 128049339,
            BelugaLiner = 128049345,
            FerDeLance = 128049351,
            Anaconda = 128049363,
            [EnumMember(Value = "federation_corvette")]
            FederalCorvette = 128049369,
            [EnumMember(Value = "cutter")]
            ImperialCutter = 128049375,
            [EnumMember(Value = "diamondback")]
            DiamondbackScout = 128671217,
            [EnumMember(Value = "empire_courier")]
            ImperialCourier = 128671223,
            [EnumMember(Value = "diamondbackxl")]
            DiamondbackExplorer = 128671831,
            [EnumMember(Value = "empire_eagle")]
            ImperialEagle = 128672138,
            [EnumMember(Value = "federation_dropship_mkii")]
            FederalAssaultShip = 128672145,
            [EnumMember(Value = "federation_gunship")]
            FederalGunship = 128672152,
            [EnumMember(Value = "viper_mkiv")]
            ViperMkIV = 128672255,
            CobraMkIV = 128672262,
            [EnumMember(Value = "independant_trader")]
            Keelback = 128672269,
            [EnumMember(Value = "asp_scout")]
            AspScout = 128672276,
            [EnumMember(Value = "type9_military")]
            Type10 = 128785619,
            [EnumMember(Value = "krait_mkii")]
            KraitMkII = 128816567,
            [EnumMember(Value = "typex")]
            AllianceChieftain = 128816574,
            [EnumMember(Value = "typex_2")]
            AllianceCrusader = 128816581,
            [EnumMember(Value = "typex_3")]
            AllianceChallenger = 128816588,
            [EnumMember(Value = "krait_light")]
            KraitPhantom = 128839281,
            Mamba = 128915979,

            NotShipsAbove = 900000000,

            unknownsaucer, // Whatever this is
            unknownsaucer_h,
            unknownsaucer_f,

            [EnumMember(Value = "carrierdockb")]
            FleetCarrier,
            oneillcylinder,
            coriolis,
            oneillorbis,
            ps_turretbasemedium_skiff_6m,

            skimmerdrone,

            assaultsuitai_class1,
            assaultsuitai_class2,
            assaultsuitai_class3,
            assaultsuitai_class4,
            assaultsuitai_class5,
            heavysuitai_class1,
            heavysuitai_class2,
            heavysuitai_class3,
            heavysuitai_class4,
            heavysuitai_class5,
            closesuitai_class1,
            closesuitai_class2,
            closesuitai_class3,
            closesuitai_class4,
            closesuitai_class5,
            lightassaultsuitai_class1,
            lightassaultsuitai_class2,
            lightassaultsuitai_class3,
            lightassaultsuitai_class4,
            lightassaultsuitai_class5,

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
