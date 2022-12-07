using Microsoft.AspNetCore.Identity;

namespace EDDatabase
{
    [Table("ApplicationUser")]
    public class ApplicationUser : IdentityUser
    {
        [ForeignKey("CommanderId")]
        public Commander? Commander { get; set; }

        public ApplicationUser(string userName) : base(userName) { }
    }
}
