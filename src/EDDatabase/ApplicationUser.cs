using Microsoft.AspNetCore.Identity;

namespace EDDatabase
{
    [Table("ApplicationUser")]
    public class ApplicationUser : IdentityUser
    {
        [ForeignKey("CommanderId")]
        public Commander? Commander { get; set; }

        [ForeignKey("CommanderId")]
        public int? CommanderId { get; set; }

        public ApplicationUser(string userName) : 
            base(userName) { }
    }
}
