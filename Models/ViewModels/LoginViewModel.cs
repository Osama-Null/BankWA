using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace BankWA.Models.ViewModels
{
    public class LoginViewModel
    {
        [DisplayName("Email")]
        [Required(ErrorMessage = "*Enter email")]
        [EmailAddress]
        [MinLength(5)]
        public string Email { get; set; }
        [Required(ErrorMessage = "*Enter password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
}
