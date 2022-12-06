﻿// <auto-generated />
using System;
using EDDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EDDatabase.Migrations
{
    [DbContext(typeof(EdDbContext))]
    partial class EdDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

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

                    b.Property<int?>("TypeId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("Updated")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("GovernmentId");

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

                    b.HasIndex("Side");

                    b.HasIndex("StarSystemId");

                    b.ToTable("WarEffort");
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
                        .WithMany("FssSignals")
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
                    b.HasOne("EDDatabase.StarSystem", "StarSystem")
                        .WithMany()
                        .HasForeignKey("StarSystemId");

                    b.Navigation("StarSystem");
                });

            modelBuilder.Entity("EDDatabase.StarSystem", b =>
                {
                    b.Navigation("FssSignals");

                    b.Navigation("ThargoidLevelHistory");
                });
#pragma warning restore 612, 618
        }
    }
}
