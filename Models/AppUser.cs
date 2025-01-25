using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BankWA.Models
{
    public class AppUser : IdentityUser
    {
        public decimal Balance { get; set; }
        public string? Img { get; set; }
    }
}
