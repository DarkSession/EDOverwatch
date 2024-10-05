using System.ComponentModel.DataAnnotations;

namespace EDDatabase;

[Table("PlayerActivity")]
[Index(nameof(Hash), IsUnique = true)]
[Index(nameof(StarSystemId), nameof(DateHour))]
public sealed class PlayerActivity
{
    [Key]
    public required long Id { get; set; }

    public required long StarSystemId { get; set; }

    public required StarSystem? StarSystem { get; set; }

    public required int DateHour { get; set; }

    [StringLength(24)]
    public required string Hash { get; set; }
}
