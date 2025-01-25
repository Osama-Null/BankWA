using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BankWA.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "*Enter name")]
        [MinLength(3)]
        public string Name { get; set; }
        // Email, ConfirmEmail, Password, ConfirmPassword, Mobile
        [Required(ErrorMessage = "*Enter email")]
        [EmailAddress]
        [MinLength(5)]
        public string Email { get; set; }
        [Required(ErrorMessage = "*Enter password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required(ErrorMessage = "*Re-nter password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "*Not match!")]
        public string ConfirmPassword { get; set; }
        [Required(ErrorMessage = "*Eneter phone")]
        [DataType(DataType.PhoneNumber)]
        public string Mobile { get; set; }
    }
}
