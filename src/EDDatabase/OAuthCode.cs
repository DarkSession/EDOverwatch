using System.ComponentModel.DataAnnotations;

namespace EDDatabase
{
    [Table("OAuthCode")]
    public class OAuthCode
    {
        [Key]
        [Column(TypeName = "varchar(128)")]
        public string State { get; set; } = string.Empty;

        [Column(TypeName = "varchar(128)")]
        public string Code { get; set; } = string.Empty;

        [Column]
        public DateTimeOffset Created { get; set; }

        public OAuthCode(string state, string code, DateTimeOffset created)
        {
            State = state;
            Code = code;
            Created = created;
        }
    }
}
