using System.ComponentModel.DataAnnotations;
using biddingServer.Models;

namespace DTO.Response.Wallet
{
    public class WalletResponseDTO
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public decimal CashPoints { get; set; }
        public decimal MeritPoints { get; set; }
        public decimal MeritRatio { get; set; } = 10m;
        public string Currency { get; set; } = "PHP";
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WalletTransactionResponseDTO
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public WalletTransactionType Type { get; set; }
        public string TypeName { get; set; } = "";
        public PointType PointType { get; set; }
        public string PointTypeName { get; set; } = "";
        public TransactionStatus Status { get; set; }
        public string StatusName { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TopUpResponseDTO
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string XenditReferenceId { get; set; } = "";
        public string? CheckoutUrl { get; set; }
        public TopUpStatus Status { get; set; }
        public string StatusName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
    public class TopUpCompleteResponseDTO : TopUpResponseDTO
    {
        public string? XenditPaymentId { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
    public class WithdrawalResponseDTO
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal NetAmount { get; set; }
        public string ChannelCode { get; set; } = "";
        public string AccountHolderName { get; set; } = "";
        public WithdrawalStatus Status { get; set; }
        public string StatusName { get; set; } = "";
        public string? FailureReason { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WalletBalanceCheckDTO
    {
        public bool CanAfford { get; set; }
        public decimal CashPoints { get; set; }
        public decimal MeritPoints { get; set; }
        public decimal RequiredCash { get; set; }
        public decimal RequiredMerit { get; set; }
        public string? ShortfallReason { get; set; }
    }
}
