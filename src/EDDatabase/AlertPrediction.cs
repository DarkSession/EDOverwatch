﻿namespace EDDatabase
{
    [Table("AlertPrediction")]
    public class AlertPrediction
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("CycleId")]
        public ThargoidCycle? Cycle { get; set; }

        [ForeignKey("MaelstromId")]
        public ThargoidMaelstrom? Maelstrom { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        [ForeignKey("StarSystemId")]
        public long? StarSystemId { get; set; }

        [Column]
        public bool AlertLikely { get; set; }

        public List<AlertPredictionAttacker>? Attackers { get; set; }

        public AlertPrediction(int id, long? starSystemId, bool alertLikely)
        {
            Id = id;
            StarSystemId = starSystemId;
            AlertLikely = alertLikely;
        }
    }
}