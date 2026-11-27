using System.ComponentModel.DataAnnotations;

namespace DTO.Request.Wallet
{
    public class TopUpRequestDTO
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public string? PaymentMethod { get; set; } // GCASH, MAYA, CARD, etc.
    }

    public class WithdrawalRequestDTO
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChannelCode { get; set; } = string.Empty; // bank / e-wallet channel

        [Required]
        [MaxLength(100)]
        public string AccountHolderName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;
    }

    public class WalletTransactionFilterDTO
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public WalletTransactionType? Type { get; set; }
        public TransactionStatus? Status { get; set; }
    }
}
