namespace EDDatabase
{
    [Table("StarSystemUpdateQueueItem")]
    public class StarSystemUpdateQueueItem
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public ulong DiscordUserId { get; set; }

        [Column]
        public ulong DiscordChannelId { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        [Column]
        public StarSystemUpdateQueueItemStatus Status { get; set; }

        [Column]
        public StarSystemUpdateQueueItemResult Result { get; set; }

        [Column]
        public StarSystemUpdateQueueItemResultBy ResultBy { get; set; }

        [Column]
        public DateTimeOffset Queued { get; set; }

        [Column]
        public DateTimeOffset? Completed { get; set; }

        public StarSystemUpdateQueueItem(int id, ulong discordUserId, ulong discordChannelId, StarSystemUpdateQueueItemStatus status, StarSystemUpdateQueueItemResult result, StarSystemUpdateQueueItemResultBy resultBy, DateTimeOffset queued, DateTimeOffset? completed)
        {
            Id = id;
            DiscordUserId = discordUserId;
            DiscordChannelId = discordChannelId;
            Status = status;
            Result = result;
            ResultBy = resultBy;
            Queued = queued;
            Completed = completed;
        }
    }

    public enum StarSystemUpdateQueueItemStatus : byte
    {
        PendingAutomaticReview = 1,
        PendingManualReview,
        PendingNotification,
        Completed,
    }

    public enum StarSystemUpdateQueueItemResult : byte
    {
        Pending = 0,
        NotUpdated,
        Updated,
    }

    public enum StarSystemUpdateQueueItemResultBy : byte
    {
        Manual = 1,
        Automatic,
    }
}
