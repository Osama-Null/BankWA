using System.ComponentModel.DataAnnotations;

namespace BankWA.Models.ViewModels
{
    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "*Enter role name")]
        [MinLength(2)]
        public string? Name { get; set; }
    }
}
