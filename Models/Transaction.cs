using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace BankWA.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; } // Primary Key

        [Required(ErrorMessage = "Enter The Amount you Need ")]
        public decimal Amount { get; set; } // Total transaction amount

        public DateTime Date { get; set; } // Transaction date

        [Required]
        [DisplayName("User ID")]
        [ForeignKey("User")]
        public string UserId { get; set; } // Foreign Key to User
        public AppUser? User { get; set; } // Navigation property

        [Required]
        public TransactionType Type { get; set; } // Transaction type (e.g., Transfer, Deposit, Withdraw)

        public string? ReceiverId { get; set; } // Optional: Receiver ID for transfer transactions
        [ForeignKey("ReceiverId")]
        public AppUser? Receiver { get; set; } // Navigation property for Receiver
    }
    public enum TransactionType
    {
        Transfer,
        Deposit,
        Withdraw
    }
}
