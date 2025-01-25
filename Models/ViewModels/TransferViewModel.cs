using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace BankWA.Models.ViewModels
{
    public class TransferViewModel
    {
        [DisplayName("Send to")]
        [Required(ErrorMessage = "*Receiver email is required")]
        [EmailAddress(ErrorMessage = "*Invalid email address")]
        public string ReceiverEmail { get; set; }

        [DisplayName("Amount")]
        [Required(ErrorMessage = "*Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "*Amount must be greater than zero")]
        public decimal Amount { get; set; }
    }
}
