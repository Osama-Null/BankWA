using Microsoft.AspNetCore.Identity;

namespace BankWA.Models
{
    public class AppUser : IdentityUser
    {
        public decimal Balance { get; set; }
        public String? Img { get; set; }
    }
}
