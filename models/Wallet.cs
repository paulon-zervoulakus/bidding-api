using System.ComponentModel.DataAnnotations;

public enum PointType
{
    Cash = 1,
    Merit = 2,
    Reward = 3
}

public enum TopUpStatus { Pending = 1, Paid = 2, Expired = 3, Failed = 4 }
public enum WithdrawalStatus { Pending = 1, Processing = 2, Completed = 3, Failed = 4, Cancelled = 5 }
public enum ReservationHoldStatus { Held = 1, Released = 2, Forfeited = 3 }
public static class WalletRatio
{
    public const decimal MeritPerCashPoint = 10m;
    public static decimal MeritToCash(decimal merit) => merit / MeritPerCashPoint;
    public static decimal CashToMerit(decimal cash) => cash * MeritPerCashPoint;
}
public enum WalletTransactionType
{
    TopUp = 1,              // Xendit payment -> cash points (+)
    Withdrawal = 2,         // cash points -> payout (-)
    WithdrawalRefund = 3,   // failed payout returned (+)
    ReservationHold = 4,    // reservation fee locked (-)
    ReservationRelease = 5, // reservation returned (+)
    ReservationForfeit = 6, // reservation consumed, not returned (no balance change, finalizes hold)
    MeritEarned = 7,        // merit awarded (+)
    MeritExpired = 8,       // optional (-)
    Adjustment = 9          // admin correction (+/-)
}

public enum TransactionStatus { Pending = 1, Completed = 2, Failed = 3, Reversed = 4 }

/**
    Wallet: one per user. It holds the current balances (CashPoints and MeritPoints).
    It's what you read to show "you have X points" and to check if a bid can be afforded.
    RowVersion prevents two simultaneous requests from spending the same points.
    IsLocked lets you freeze it.
*/
public class Wallet
{
    public Guid Id { get; set; }
    public int UserId { get; set; }                 // unique index

    public decimal CashPoints { get; set; }          // withdrawable (1 point = 1 PHP)
    public decimal MeritPoints { get; set; }         // not withdrawable

    public string Currency { get; set; } = "PHP";
    public bool IsLocked { get; set; }               // freeze on fraud/dispute

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!; // optimistic concurrency

    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
/**
    WalletTransaction: the ledger.
    Every change to a balance adds a row here and never edits or deletes one.
    Each row records what happened (Type), which balance (PointType), how much (Amount, signed), and the balance afterwards.
    If a user disputes their balance, you replay this table to prove it.
    IdempotencyKey makes sure the same Xendit webhook, delivered twice, only credits once.
    */
public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = default!;

    public WalletTransactionType Type { get; set; }
    public PointType PointType { get; set; }
    public TransactionStatus Status { get; set; }

    public decimal Amount { get; set; }              // signed: + credit, - debit
    public decimal BalanceAfter { get; set; }        // balance of that PointType after applying

    // Merit-only fields (set for merit rows)
    public decimal? MeritRate { get; set; }          // snapshot of merit-to-cash rate at transaction time
    public decimal? CashEquivalent { get; set; }     // cash equivalent for merit transactions

    // What caused it (polymorphic link)
    public string? ReferenceType { get; set; }       // "TopUp", "Withdrawal", "Reservation", "Auction"
    public Guid? ReferenceId { get; set; }

    public string IdempotencyKey { get; set; } = default!; // unique index; also use Xendit event id for webhooks
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}
/**
    WalletTopUp: tracks a Xendit payment attempt.
    When the user taps "Add cash", you create a row with status Pending and the checkout URL.
    Only when the webhook confirms payment do you mark it Paid and add the cash points through a WalletTransaction.
    It also covers payments that were never completed (Expired, Failed), which don't belong in the ledger.
    */
public class WalletTopUp
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }

    public decimal Amount { get; set; }              // PHP charged = cash points credited
    public decimal Fee { get; set; }                 // Xendit fee, if you pass it on

    public string XenditReferenceId { get; set; } = default!; // your external_id / reference_id
    public string? XenditPaymentId { get; set; }     // invoice / payment request id
    public string? PaymentMethod { get; set; }       // GCASH, MAYA, CARD, etc.
    public string? CheckoutUrl { get; set; }

    public TopUpStatus Status { get; set; }          // Pending, Paid, Expired, Failed
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
/**
    WalletWithdrawal: tracks a cash-out through Xendit payouts.
    It stores the destination (bank or e-wallet), the fee, and the status.
    Points are deducted up front.
    If the payout fails, you add a WithdrawalRefund ledger row to return them.
    */
public class WalletWithdrawal
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }

    public decimal Amount { get; set; }              // cash points debited
    public decimal Fee { get; set; }
    public decimal NetAmount { get; set; }           // cash paid out

    public string ChannelCode { get; set; } = default!;      // bank / e-wallet channel
    public string AccountHolderName { get; set; } = default!;
    public string AccountNumber { get; set; } = default!;    // encrypt at rest

    public string XenditReferenceId { get; set; } = default!;
    public string? XenditPayoutId { get; set; }

    public WithdrawalStatus Status { get; set; }     // Pending, Processing, Completed, Failed, Cancelled
    public string? FailureReason { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Funds held for a bid reservation
public class ReservationHold
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid AuctionId { get; set; }              // or BidId, whichever you reserve against

    public decimal CashAmount { get; set; }
    public decimal MeritAmount { get; set; }
    public decimal MeritRate { get; set; }           // snapshot at hold time

    public ReservationHoldStatus Status { get; set; } // Held, Released, Forfeited
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}