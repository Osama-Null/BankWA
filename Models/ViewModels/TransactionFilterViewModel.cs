using System.ComponentModel.DataAnnotations;

namespace BankWA.Models.ViewModels
{
    public class TransactionFilterViewModel
    {
        public string? SearchString { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public decimal? MinAmount { get; set; }

        public decimal? MaxAmount { get; set; }
    }
}
