﻿// <auto-generated />
using System;
using EDDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EDDatabase.Migrations
{
    [DbContext(typeof(EdDbContext))]
    [Migration("20221217233702_FixTableName")]
    partial class FixTableName
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("EDDatabase.ApplicationUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<int?>("CommanderId")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .HasColumnType("longtext");

                    b.Property<string>("Email")
                        .HasColumnType("longtext");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("NormalizedEmail")
                        .HasColumnType("longtext");

                    b.Property<string>("NormalizedUserName")
                        .HasColumnType("longtext");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("longtext");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("longtext");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("longtext");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("UserName")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("CommanderId");

                    b.ToTable("ApplicationUser");
                });

            modelBuilder.Entity("EDDatabase.Commander", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<long>("FDevCustomerId")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsInLiveVersion")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateOnly>("JournalDay")
                        .HasColumnType("date");

                    b.Property<DateTimeOffset>("JournalLastActivity")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("JournalLastLine")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("JournalLastProcessed")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.Property<string>("OAuthAccessToken")
                        .IsRequired()
                        .HasColumnType("varchar(4096)");

                    b.Property<DateTimeOffset>("OAuthCreated")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("OAuthRefreshToken")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.Property<byte>("OAuthStatus")
                        .HasColumnType("tinyint unsigned");

                    b.Property<string>("OAuthTokenType")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.Property<long?>("StationId")
                        .HasColumnType("bigint");

                    b.Property<long?>("SystemId")
                        .HasColumnType("bigint");

                    b.Property<string>("UserId")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("FDevCustomerId")
                        .IsUnique();

                    b.HasIndex("StationId");

                    b.HasIndex("SystemId");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Commander");
                });

            modelBuilder.Entity("EDDatabase.CommanderDeferredJournalEvent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("CommanderId")
                        .HasColumnType("int");

                    b.Property<string>("Event")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.Property<string>("Journal")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<byte>("Source")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte>("Status")
                        .HasColumnType("tinyint unsigned");

                    b.Property<long?>("SystemId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("CommanderId");

                    b.HasIndex("Status");

                    b.HasIndex("SystemId");

                    b.ToTable("CommanderDeferredJournalEvent");
                });

            modelBuilder.Entity("EDDatabase.CommanderMission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("CommanderId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<long>("MissionId")
                        .HasColumnType("bigint");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<long?>("SystemId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("CommanderId");

                    b.HasIndex("MissionId")
                        .IsUnique();

                    b.HasIndex("SystemId");

                    b.ToTable("CommanderMission");
                });

            modelBuilder.Entity("EDDatabase.DcohDiscordUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<ulong>("DiscordId")
                        .HasColumnType("bigint unsigned");

                    b.Property<int?>("FactionId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("FactionJoined")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("DiscordId")
                        .IsUnique();

                    b.HasIndex("FactionId");

                    b.ToTable("DcohDiscordUser");
                });

            modelBuilder.Entity("EDDatabase.DcohFaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("CreatedById")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.Property<string>("Short")
                        .IsRequired()
                        .HasColumnType("varchar(8)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedById");

                    b.HasIndex("Short")
                        .IsUnique();

                    b.ToTable("DcohFaction");
                });

            modelBuilder.Entity("EDDatabase.DcohFactionOperation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("CreatedById")
                        .HasColumnType("int");

                    b.Property<int?>("FactionId")
                        .HasColumnType("int");

                    b.Property<long?>("StarSystemId")
                        .HasColumnType("bigint");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("CreatedById");

                    b.HasIndex("FactionId");

                    b.HasIndex("StarSystemId");

                    b.ToTable("DcohFactionOperation");
                });

            modelBuilder.Entity("EDDatabase.Economy", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Economy");
                });

            modelBuilder.Entity("EDDatabase.FactionAllegiance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("FactionAllegiance");
                });

            modelBuilder.Entity("EDDatabase.FactionGovernment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("FactionGovernment");
                });

            modelBuilder.Entity("EDDatabase.OAuthCode", b =>
                {
                    b.Property<string>("State")
                        .HasColumnType("varchar(128)");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("varchar(128)");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("datetime(6)");

                    b.HasKey("State");

                    b.ToTable("OAuthCode");
                });

            modelBuilder.Entity("EDDatabase.StarSystem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<int?>("AllegianceId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("LocationX")
                        .HasColumnType("decimal(14,6)");

                    b.Property<decimal>("LocationY")
                        .HasColumnType("decimal(14,6)");

                    b.Property<decimal>("LocationZ")
                        .HasColumnType("decimal(14,6)");

                    b.Property<int?>("MaelstromId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(512)");

                    b.Property<long>("OriginalPopulation")
                        .HasColumnType("bigint");

                    b.Property<long>("Population")
                        .HasColumnType("bigint");

                    b.Property<int?>("SecurityId")
                        .HasColumnType("int");

                    b.Property<long>("SystemAddress")
                        .HasColumnType("bigint");

                    b.Property<int?>("ThargoidLevelId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("Updated")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("WarRelevantSystem")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.HasIndex("AllegianceId");

                    b.HasIndex("MaelstromId");

                    b.HasIndex("SecurityId");

                    b.HasIndex("SystemAddress")
                        .IsUnique();

                    b.HasIndex("ThargoidLevelId");

                    b.HasIndex("WarRelevantSystem");

                    b.HasIndex("LocationX", "LocationY", "LocationZ");

                    b.ToTable("StarSystem");
                });

            modelBuilder.Entity("EDDatabase.StarSystemFssSignal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("FirstSeen")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset>("LastSeen")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(512)");

                    b.Property<long?>("StarSystemId")
                        .HasColumnType("bigint");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("StarSystemId");

                    b.ToTable("StarSystemFssSignal");
                });

            modelBuilder.Entity("EDDatabase.StarSystemSecurity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("StarSystemSecurity");
                });

            modelBuilder.Entity("EDDatabase.StarSystemThargoidLevel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("CycleEndId")
                        .HasColumnType("int");

                    b.Property<int?>("CycleStartId")
                        .HasColumnType("int");

                    b.Property<int?>("MaelstromId")
                        .HasColumnType("int");

                    b.Property<short?>("Progress")
                        .HasColumnType("smallint");

                    b.Property<long?>("StarSystemId")
                        .HasColumnType("bigint");

                    b.Property<byte>("State")
                        .HasColumnType("tinyint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("CycleEndId");

                    b.HasIndex("CycleStartId");

                    b.HasIndex("MaelstromId");

                    b.HasIndex("StarSystemId");

                    b.HasIndex("State");

                    b.ToTable("StarSystemThargoidLevel");
                });

            modelBuilder.Entity("EDDatabase.Station", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("DistanceFromStarLS")
                        .HasColumnType("decimal(14,6)");

                    b.Property<int?>("GovernmentId")
                        .HasColumnType("int");

                    b.Property<short>("LandingPadLarge")
                        .HasColumnType("smallint");

                    b.Property<short>("LandingPadMedium")
                        .HasColumnType("smallint");

                    b.Property<short>("LandingPadSmall")
                        .HasColumnType("smallint");

                    b.Property<long>("MarketId")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(512)");

                    b.Property<int?>("PrimaryEconomyId")
                        .HasColumnType("int");

                    b.Property<int?>("SecondaryEconomyId")
                        .HasColumnType("int");

                    b.Property<long?>("StarSystemId")
                        .HasColumnType("bigint");

                    b.Property<byte>("State")
                        .HasColumnType("tinyint unsigned");

                    b.Property<int?>("TypeId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("Updated")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("GovernmentId");

                    b.HasIndex("MarketId");

                    b.HasIndex("PrimaryEconomyId");

                    b.HasIndex("SecondaryEconomyId");

                    b.HasIndex("StarSystemId");

                    b.HasIndex("TypeId");

                    b.ToTable("Station");
                });

            modelBuilder.Entity("EDDatabase.StationType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.Property<string>("NameEnglish")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("StationType");
                });

            modelBuilder.Entity("EDDatabase.ThargoidCycle", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("End")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset>("Start")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("ThargoidCycle");
                });

            modelBuilder.Entity("EDDatabase.ThargoidMaelstrom", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<decimal>("InfluenceSphere")
                        .HasColumnType("decimal(14,6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(256)");

                    b.Property<long?>("StarSystemId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("Updated")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("StarSystemId");

                    b.ToTable("ThargoidMaelstrom");
                });

            modelBuilder.Entity("EDDatabase.WarEffort", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<long>("Amount")
                        .HasColumnType("bigint");

                    b.Property<int?>("CommanderId")
                        .HasColumnType("int");

                    b.Property<DateOnly>("Date")
                        .HasColumnType("date");

                    b.Property<byte>("Side")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte>("Source")
                        .HasColumnType("tinyint unsigned");

                    b.Property<long?>("StarSystemId")
                        .HasColumnType("bigint");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("CommanderId");

                    b.HasIndex("Side");

                    b.HasIndex("StarSystemId");

                    b.ToTable("WarEffort");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ClaimType")
                        .HasColumnType("longtext");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("longtext");

                    b.Property<string>("UserId")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("IdentityUserClaims");
                });

            modelBuilder.Entity("EDDatabase.ApplicationUser", b =>
                {
                    b.HasOne("EDDatabase.Commander", "Commander")
                        .WithMany()
                        .HasForeignKey("CommanderId");

                    b.Navigation("Commander");
                });

            modelBuilder.Entity("EDDatabase.Commander", b =>
                {
                    b.HasOne("EDDatabase.Station", "Station")
                        .WithMany()
                        .HasForeignKey("StationId");

                    b.HasOne("EDDatabase.StarSystem", "System")
                        .WithMany()
                        .HasForeignKey("SystemId");

                    b.HasOne("EDDatabase.ApplicationUser", "User")
                        .WithOne()
                        .HasForeignKey("EDDatabase.Commander", "UserId");

                    b.Navigation("Station");

                    b.Navigation("System");

                    b.Navigation("User");
                });

            modelBuilder.Entity("EDDatabase.CommanderDeferredJournalEvent", b =>
                {
                    b.HasOne("EDDatabase.Commander", "Commander")
                        .WithMany()
                        .HasForeignKey("CommanderId");

                    b.HasOne("EDDatabase.StarSystem", "System")
                        .WithMany()
                        .HasForeignKey("SystemId");

                    b.Navigation("Commander");

                    b.Navigation("System");
                });

            modelBuilder.Entity("EDDatabase.CommanderMission", b =>
                {
                    b.HasOne("EDDatabase.Commander", "Commander")
                        .WithMany()
                        .HasForeignKey("CommanderId");

                    b.HasOne("EDDatabase.StarSystem", "System")
                        .WithMany()
                        .HasForeignKey("SystemId");

                    b.Navigation("Commander");

                    b.Navigation("System");
                });

            modelBuilder.Entity("EDDatabase.DcohDiscordUser", b =>
                {
                    b.HasOne("EDDatabase.DcohFaction", "Faction")
                        .WithMany()
                        .HasForeignKey("FactionId");

                    b.Navigation("Faction");
                });

            modelBuilder.Entity("EDDatabase.DcohFaction", b =>
                {
                    b.HasOne("EDDatabase.DcohDiscordUser", "CreatedBy")
                        .WithMany()
                        .HasForeignKey("CreatedById");

                    b.Navigation("CreatedBy");
                });

            modelBuilder.Entity("EDDatabase.DcohFactionOperation", b =>
                {
                    b.HasOne("EDDatabase.DcohDiscordUser", "CreatedBy")
                        .WithMany()
                        .HasForeignKey("CreatedById");

                    b.HasOne("EDDatabase.DcohFaction", "Faction")
                        .WithMany()
                        .HasForeignKey("FactionId");

                    b.HasOne("EDDatabase.StarSystem", "StarSystem")
                        .WithMany("FactionOperations")
                        .HasForeignKey("StarSystemId");

                    b.Navigation("CreatedBy");

                    b.Navigation("Faction");

                    b.Navigation("StarSystem");
                });

            modelBuilder.Entity("EDDatabase.StarSystem", b =>
                {
                    b.HasOne("EDDatabase.FactionAllegiance", "Allegiance")
                        .WithMany()
                        .HasForeignKey("AllegianceId");

                    b.HasOne("EDDatabase.ThargoidMaelstrom", "Maelstrom")
                        .WithMany()
                        .HasForeignKey("MaelstromId");

                    b.HasOne("EDDatabase.StarSystemSecurity", "Security")
                        .WithMany()
                        .HasForeignKey("SecurityId");

                    b.HasOne("EDDatabase.StarSystemThargoidLevel", "ThargoidLevel")
                        .WithMany()
                        .HasForeignKey("ThargoidLevelId");

                    b.Navigation("Allegiance");

                    b.Navigation("Maelstrom");

                    b.Navigation("Security");

                    b.Navigation("ThargoidLevel");
                });

            modelBuilder.Entity("EDDatabase.StarSystemFssSignal", b =>
                {
                    b.HasOne("EDDatabase.StarSystem", "StarSystem")
                        .WithMany()
                        .HasForeignKey("StarSystemId");

                    b.Navigation("StarSystem");
                });

            modelBuilder.Entity("EDDatabase.StarSystemThargoidLevel", b =>
                {
                    b.HasOne("EDDatabase.ThargoidCycle", "CycleEnd")
                        .WithMany()
                        .HasForeignKey("CycleEndId");

                    b.HasOne("EDDatabase.ThargoidCycle", "CycleStart")
                        .WithMany()
                        .HasForeignKey("CycleStartId");

                    b.HasOne("EDDatabase.ThargoidMaelstrom", "Maelstrom")
                        .WithMany()
                        .HasForeignKey("MaelstromId");

                    b.HasOne("EDDatabase.StarSystem", "StarSystem")
                        .WithMany("ThargoidLevelHistory")
                        .HasForeignKey("StarSystemId");

                    b.Navigation("CycleEnd");

                    b.Navigation("CycleStart");

                    b.Navigation("Maelstrom");

                    b.Navigation("StarSystem");
                });

            modelBuilder.Entity("EDDatabase.Station", b =>
                {
                    b.HasOne("EDDatabase.FactionGovernment", "Government")
                        .WithMany()
                        .HasForeignKey("GovernmentId");

                    b.HasOne("EDDatabase.Economy", "PrimaryEconomy")
                        .WithMany()
                        .HasForeignKey("PrimaryEconomyId");

                    b.HasOne("EDDatabase.Economy", "SecondaryEconomy")
                        .WithMany()
                        .HasForeignKey("SecondaryEconomyId");

                    b.HasOne("EDDatabase.StarSystem", "StarSystem")
                        .WithMany()
                        .HasForeignKey("StarSystemId");

                    b.HasOne("EDDatabase.StationType", "Type")
                        .WithMany()
                        .HasForeignKey("TypeId");

                    b.Navigation("Government");

                    b.Navigation("PrimaryEconomy");

                    b.Navigation("SecondaryEconomy");

                    b.Navigation("StarSystem");

                    b.Navigation("Type");
                });

            modelBuilder.Entity("EDDatabase.ThargoidMaelstrom", b =>
                {
                    b.HasOne("EDDatabase.StarSystem", "StarSystem")
                        .WithMany()
                        .HasForeignKey("StarSystemId");

                    b.Navigation("StarSystem");
                });

            modelBuilder.Entity("EDDatabase.WarEffort", b =>
                {
                    b.HasOne("EDDatabase.Commander", "Commander")
                        .WithMany()
                        .HasForeignKey("CommanderId");

                    b.HasOne("EDDatabase.StarSystem", "StarSystem")
                        .WithMany()
                        .HasForeignKey("StarSystemId");

                    b.Navigation("Commander");

                    b.Navigation("StarSystem");
                });

            modelBuilder.Entity("EDDatabase.StarSystem", b =>
                {
                    b.Navigation("FactionOperations");

                    b.Navigation("ThargoidLevelHistory");
                });
#pragma warning restore 612, 618
        }
    }
}
