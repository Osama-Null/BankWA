using System.ComponentModel.DataAnnotations;

namespace BankWA.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "*Enter password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [DataType(DataType.PhoneNumber)]
        public string Mobile { get; set; }
        public IFormFile? Img { get; set; }
        public string Name { get; set; }
        [EmailAddress]
        public string Email { get; set; }
    }
}
