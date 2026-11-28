using biddingServer.Models;
using DTO.Request.Wallet;
using DTO.Response.Wallet;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

using biddingServer.services.xendit;

namespace biddingServer.services.wallet
{
    public interface IWalletService
    {
        Task<Wallet> GetOrCreateAsync(int userId);
        Task<WalletTransaction> ApplyAsync(
            Wallet wallet,
            WalletTransactionType type,
            PointType pointType,
            decimal amount,
            string idempotencyKey,
            string? referenceType = null,
            Guid? referenceId = null,
            string? description = null,
            decimal? meritRate = null,
            decimal? cashEquivalent = null,
            TransactionStatus status = TransactionStatus.Completed,
            CancellationToken ct = default);
        Task<WalletResponseDTO?> GetWalletAsync(int userId);
        Task<List<WalletTransactionResponseDTO>> GetTransactionsAsync(int userId, DateTime? before = null, PointType? pointType = null, int limit = 20);
        Task<TopUpResponseDTO> CreateTopUpAsync(int userId, TopUpRequestDTO dto);
        Task<WithdrawalResponseDTO> CreateWithdrawalAsync(int userId, WithdrawalRequestDTO dto);
        Task<WalletBalanceCheckDTO> CanAffordAsync(int userId, decimal requiredCash, decimal requiredMerit);
        Task<bool> CreditMeritAsync(int userId, decimal merit, string description, string? referenceType = null, Guid? referenceId = null, string? idempotencyKey = null);
        Task<bool> DebitCashAsync(int userId, decimal amount, string description, string? referenceType = null, Guid? referenceId = null, string? idempotencyKey = null);
        Task<ReservationHold> HoldReservationAsync(Guid walletId, Guid auctionId, decimal feeCashPoints);
        Task ReleaseReservationAsync(Guid holdId);
        Task ForfeitReservationAsync(Guid holdId);
        Task GrantMeritAsync(Guid walletId, decimal meritAmount, string source, Guid? referenceId = null);
        Task<List<TopUpCompleteResponseDTO>> GetTopUpsAsync(int userId, DateTime? before = null, int limit = 20);
        Task<TopUpCompleteResponseDTO?> VerifyTopUpAsync(int userId, Guid topUpId, CancellationToken ct = default);
        Task<WalletTopUp?> CreditTopUpAsync(Guid topUpId, decimal paidAmount, string invoiceId, CancellationToken ct = default);
    }
    public record TopUpVerifyResult
    (
        string Status,
        bool Credited,
        decimal CashPoints
    );

    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _context;
        private readonly IXenditService _xendit;
        private readonly IConfiguration _config;
        private readonly IDataProtectionProvider _dataProtection;
        private IDataProtector _protector => _dataProtection.CreateProtector("WalletWithdrawal.AccountNumber");

        public WalletService(ApplicationDbContext context, IXenditService xendit, IConfiguration config, IDataProtectionProvider dataProtection)
        {
            _context = context;
            _xendit = xendit;
            _config = config;
            _dataProtection = dataProtection;
        }
        private string ProtectAccountNumber(string plain) => _protector.Protect(plain);
        private string UnprotectAccountNumber(string cipher) => _protector.Unprotect(cipher);
        public async Task<Wallet> GetOrCreateAsync(int userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet != null) return wallet;
            try
            {
                wallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CashPoints = 0,
                    MeritPoints = 0,
                    Currency = "PHP",
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
                return wallet;
            }
            catch (DbUpdateException)
            {
                return await _context.Wallets.FirstAsync(w => w.UserId == userId);
            }
        }
        public async Task<WalletTransaction> ApplyAsync(
            Wallet wallet,
            WalletTransactionType type,
            PointType pointType,
            decimal amount,
            string idempotencyKey,
            string? referenceType = null,
            Guid? referenceId = null,
            string? description = null,
            decimal? meritRate = null,
            decimal? cashEquivalent = null,
            TransactionStatus status = TransactionStatus.Completed,
            CancellationToken ct = default)
        {
            var existing = await _context.WalletTransactions.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
            if (existing != null) return existing;

            if (amount < 0 && wallet.IsLocked)
                throw new InvalidOperationException("Wallet is locked.");

            decimal newBalance;
            if (pointType == PointType.Cash)
            {
                newBalance = wallet.CashPoints + amount;
                if (newBalance < 0) throw new InvalidOperationException("Insufficient cash points.");
                wallet.CashPoints = newBalance;
            }
            else
            {
                newBalance = wallet.MeritPoints + amount;
                if (newBalance < 0) throw new InvalidOperationException("Insufficient merit points.");
                wallet.MeritPoints = newBalance;
            }
            wallet.UpdatedAt = DateTime.UtcNow;

            var tx = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = type,
                PointType = pointType,
                Status = status,
                Amount = amount,
                BalanceAfter = newBalance,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                IdempotencyKey = idempotencyKey,
                Description = description,
                MeritRate = (pointType == PointType.Merit) ? (meritRate ?? WalletRatio.MeritPerCashPoint) : meritRate,
                CashEquivalent = (pointType == PointType.Merit) ? (cashEquivalent ?? WalletRatio.MeritToCash(amount >= 0 ? amount : -amount)) : cashEquivalent,
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(tx);
            return tx;
        }
        private async Task<T> WithRetry<T>(Func<Task<T>> action, int attempts = 3)
        {
            for (int i = 0; i < attempts; i++)
            {
                try { return await action(); }
                catch (DbUpdateConcurrencyException)
                {
                    if (i == attempts - 1) throw;
                    await Task.Delay(50 * (i + 1));
                }
            }
            throw new InvalidOperationException("unreachable");
        }
        public async Task<WalletResponseDTO?> GetWalletAsync(int userId)
        {
            var wallet = await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) return null;
            return new WalletResponseDTO
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                CashPoints = wallet.CashPoints,
                MeritPoints = wallet.MeritPoints,
                MeritRatio = WalletRatio.MeritPerCashPoint,
                Currency = wallet.Currency,
                IsLocked = wallet.IsLocked,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt
            };
        }
        public async Task<List<WalletTransactionResponseDTO>> GetTransactionsAsync(int userId, DateTime? before = null, PointType? pointType = null, int limit = 20)
        {
            var wallet = await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) return new List<WalletTransactionResponseDTO>();
            limit = Math.Clamp(limit, 1, 100);
            var query = _context.WalletTransactions.AsNoTracking().Where(t => t.WalletId == wallet.Id);
            if (before.HasValue) query = query.Where(t => t.CreatedAt < before.Value);
            if (pointType.HasValue) query = query.Where(t => t.PointType == pointType.Value);
            return await query.OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .Select(t => new WalletTransactionResponseDTO
                {
                    Id = t.Id,
                    WalletId = t.WalletId,
                    Type = t.Type,
                    TypeName = t.Type.ToString(),
                    PointType = t.PointType,
                    PointTypeName = t.PointType.ToString(),
                    Status = t.Status,
                    StatusName = t.Status.ToString(),
                    Amount = t.Amount,
                    BalanceAfter = t.BalanceAfter,
                    ReferenceType = t.ReferenceType,
                    ReferenceId = t.ReferenceId,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                }).ToListAsync();
        }
        public async Task<List<TopUpCompleteResponseDTO>> GetTopUpsAsync(int userId, DateTime? before = null, int limit = 20)
        {
            var wallet = await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) return new List<TopUpCompleteResponseDTO>();
            limit = Math.Clamp(limit, 1, 100);
            var query = _context.WalletTopUps.AsNoTracking().Where(t => t.WalletId == wallet.Id);
            if (before.HasValue) query = query.Where(t => t.CreatedAt < before.Value);
            return await query.OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .Select(t => new TopUpCompleteResponseDTO
                {
                    Id = t.Id,
                    WalletId = t.WalletId,
                    Amount = t.Amount,
                    Fee = t.Fee,
                    XenditReferenceId = t.XenditReferenceId,
                    XenditPaymentId = t.XenditPaymentId,
                    PaymentMethod = t.PaymentMethod,
                    CheckoutUrl = t.CheckoutUrl,
                    Status = t.Status,
                    PaidAt = t.PaidAt,
                    ExpiresAt = t.ExpiresAt,
                    CreatedAt = t.CreatedAt
                }).ToListAsync();
        }
        public async Task<TopUpResponseDTO> CreateTopUpAsync(int userId, TopUpRequestDTO dto)
        {
            var wallet = await GetOrCreateAsync(userId);
            if (wallet.IsLocked) throw new InvalidOperationException("Wallet is locked.");

            var minAmount = _config.GetValue<decimal>("Xendit:TopUpMinAmount", 100);
            var maxAmount = _config.GetValue<decimal>("Xendit:TopUpMaxAmount", 500000);
            if (dto.Amount < minAmount || dto.Amount > maxAmount)
                throw new InvalidOperationException($"Amount must be between {minAmount} and {maxAmount}.");

            var xenditRef = $"topup:{wallet.Id}:{Guid.NewGuid():N}";
            var uiHost = (_config["UI_HOST"] == "" || _config["UI_PORT"] == "" || _config["UI_PORT"] == "") ?
                "http://localhost:3000" :
                $"{_config["UI_PROTOCOL"] ?? "http"}://{_config["UI_HOST"]}:{_config["UI_PORT"]}";

            var successUrl = $"{uiHost}/account/?tab=wallet&topup=success";
            var failureUrl = $"{uiHost}/account/?tab=wallet&topup=failed";

            var invoice = await _xendit.CreateInvoiceAsync(
                dto.Amount,
                xenditRef,
                $"Wallet top-up for user {userId}",
                successUrl,
                failureUrl
            );

            var topUp = new WalletTopUp
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = dto.Amount,
                Fee = 0,
                XenditReferenceId = xenditRef,
                XenditPaymentId = invoice.Id,
                PaymentMethod = dto.PaymentMethod,
                CheckoutUrl = invoice.InvoiceUrl,
                Status = TopUpStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTopUps.Add(topUp);
            await _context.SaveChangesAsync();
            return new TopUpResponseDTO
            {
                Id = topUp.Id,
                WalletId = topUp.WalletId,
                Amount = topUp.Amount,
                Fee = topUp.Fee,
                XenditReferenceId = topUp.XenditReferenceId,
                CheckoutUrl = topUp.CheckoutUrl,
                Status = topUp.Status,
                StatusName = topUp.Status.ToString(),
                CreatedAt = topUp.CreatedAt
            };
        }
        public async Task<WithdrawalResponseDTO> CreateWithdrawalAsync(int userId, WithdrawalRequestDTO dto)
        {
            var wallet = await GetOrCreateAsync(userId);
            if (wallet.IsLocked) throw new InvalidOperationException("Wallet is locked.");
            if (wallet.CashPoints < dto.Amount) throw new InvalidOperationException("Insufficient cash points.");

            var minAmount = _config.GetValue<decimal>("Xendit:WithdrawalMinAmount", 100);
            if (dto.Amount < minAmount) throw new InvalidOperationException($"Minimum withdrawal amount is {minAmount}.");

            var feePercent = _config.GetValue<decimal>("Xendit:WithdrawalFeePercent", 2);
            var fee = dto.Amount * feePercent / 100m;
            var netAmount = dto.Amount - fee;

            var xenditRef = $"withdrawal:{Guid.NewGuid():N}";
            var withdrawal = new WalletWithdrawal
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = dto.Amount,
                Fee = fee,
                NetAmount = netAmount,
                ChannelCode = dto.ChannelCode,
                AccountHolderName = dto.AccountHolderName,
                AccountNumber = ProtectAccountNumber(dto.AccountNumber),
                XenditReferenceId = xenditRef,
                Status = WithdrawalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var idempotencyKey = $"withdrawal:{withdrawal.Id}";
            await ApplyAsync(wallet, WalletTransactionType.Withdrawal, PointType.Cash, -dto.Amount, idempotencyKey, "Withdrawal", withdrawal.Id, $"Withdrawal to {dto.ChannelCode}", status: TransactionStatus.Pending);

            _context.WalletWithdrawals.Add(withdrawal);
            await _context.SaveChangesAsync();

            return new WithdrawalResponseDTO
            {
                Id = withdrawal.Id,
                WalletId = withdrawal.WalletId,
                Amount = withdrawal.Amount,
                Fee = withdrawal.Fee,
                NetAmount = withdrawal.NetAmount,
                ChannelCode = withdrawal.ChannelCode,
                AccountHolderName = withdrawal.AccountHolderName,
                Status = withdrawal.Status,
                StatusName = withdrawal.Status.ToString(),
                CreatedAt = withdrawal.CreatedAt
            };
        }
        public async Task<WalletBalanceCheckDTO> CanAffordAsync(int userId, decimal requiredCash, decimal requiredMerit)
        {
            var wallet = await _context.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                return new WalletBalanceCheckDTO { CanAfford = false, RequiredCash = requiredCash, RequiredMerit = requiredMerit, ShortfallReason = "Wallet not found." };
            }
            var reasons = new List<string>();
            if (wallet.CashPoints < requiredCash) reasons.Add($"Insufficient cash points.");
            if (wallet.MeritPoints < requiredMerit) reasons.Add($"Insufficient merit points.");
            return new WalletBalanceCheckDTO
            {
                CanAfford = reasons.Count == 0,
                CashPoints = wallet.CashPoints,
                MeritPoints = wallet.MeritPoints,
                RequiredCash = requiredCash,
                RequiredMerit = requiredMerit,
                ShortfallReason = reasons.Count > 0 ? string.Join(" ", reasons) : null
            };
        }
        public async Task<bool> CreditMeritAsync(int userId, decimal merit, string description, string? referenceType = null, Guid? referenceId = null, string? idempotencyKey = null)
        {
            var wallet = await GetOrCreateAsync(userId);
            var key = idempotencyKey ?? $"merit:{Guid.NewGuid():N}";
            await ApplyAsync(wallet, WalletTransactionType.MeritEarned, PointType.Merit, merit, key, referenceType, referenceId, description);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DebitCashAsync(int userId, decimal amount, string description, string? referenceType = null, Guid? referenceId = null, string? idempotencyKey = null)
        {
            var wallet = await GetOrCreateAsync(userId);
            var key = idempotencyKey ?? $"debit:{Guid.NewGuid():N}";
            try
            {
                await ApplyAsync(wallet, WalletTransactionType.Adjustment, PointType.Cash, -amount, key, referenceType, referenceId, description);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }
        public async Task<ReservationHold> HoldReservationAsync(Guid walletId, Guid auctionId, decimal feeCashPoints)
        {
            return await WithRetry(async () =>
            {
                var wallet = await _context.Wallets.FirstAsync(w => w.Id == walletId);
                if (wallet.IsLocked) throw new InvalidOperationException("Wallet is locked.");

                // Spend merit first (10:1 ratio)
                var meritAvailable = wallet.MeritPoints;
                var meritNeeded = feeCashPoints * WalletRatio.MeritPerCashPoint;
                var meritToUse = Math.Min(meritAvailable, meritNeeded);
                var cashToUse = feeCashPoints - WalletRatio.MeritToCash(meritToUse);

                if (cashToUse < 0) cashToUse = 0;
                if (wallet.CashPoints < cashToUse) throw new InvalidOperationException("Insufficient cash points.");
                if (wallet.MeritPoints < meritToUse) throw new InvalidOperationException("Insufficient merit points.");

                var hold = new ReservationHold
                {
                    Id = Guid.NewGuid(),
                    WalletId = walletId,
                    AuctionId = auctionId,
                    CashAmount = cashToUse,
                    MeritAmount = meritToUse,
                    MeritRate = WalletRatio.MeritPerCashPoint,
                    Status = ReservationHoldStatus.Held,
                    CreatedAt = DateTime.UtcNow
                };

                var holdCashKey = $"hold:{hold.Id}:cash";
                var holdMeritKey = $"hold:{hold.Id}:merit";

                if (cashToUse > 0)
                    await ApplyAsync(wallet, WalletTransactionType.ReservationHold, PointType.Cash, -cashToUse, holdCashKey, "Reservation", hold.Id, $"Reservation hold for auction {auctionId}");
                if (meritToUse > 0)
                    await ApplyAsync(wallet, WalletTransactionType.ReservationHold, PointType.Merit, -meritToUse, holdMeritKey, "Reservation", hold.Id, $"Reservation hold for auction {auctionId}", meritRate: WalletRatio.MeritPerCashPoint);

                _context.ReservationHolds.Add(hold);
                await _context.SaveChangesAsync();
                return hold;
            });
        }
        public async Task ReleaseReservationAsync(Guid holdId)
        {
            await WithRetryAsync(async () =>
            {
                var hold = await _context.ReservationHolds.FirstAsync(h => h.Id == holdId);

                if (hold.Status != ReservationHoldStatus.Held)
                    throw new InvalidOperationException($"Hold is not in Held status: {hold.Status}");

                var wallet = await _context.Wallets.FirstAsync(w => w.Id == hold.WalletId);
                var releaseCashKey = $"release:{hold.Id}:cash";
                var releaseMeritKey = $"release:{hold.Id}:merit";

                if (hold.CashAmount > 0)
                    await ApplyAsync(wallet, WalletTransactionType.ReservationRelease, PointType.Cash, hold.CashAmount, releaseCashKey, "Reservation", hold.Id, $"Reservation release for auction {hold.AuctionId}");
                if (hold.MeritAmount > 0)
                    await ApplyAsync(wallet, WalletTransactionType.ReservationRelease, PointType.Merit, hold.MeritAmount, releaseMeritKey, "Reservation", hold.Id, $"Reservation release for auction {hold.AuctionId}", meritRate: hold.MeritRate);

                hold.Status = ReservationHoldStatus.Released;
                hold.ResolvedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            });
        }
        public async Task ForfeitReservationAsync(Guid holdId)
        {
            await WithRetryAsync(async () =>
            {
                var hold = await _context.ReservationHolds.FirstAsync(h => h.Id == holdId);

                if (hold.Status != ReservationHoldStatus.Held)
                    throw new InvalidOperationException($"Hold is not in Held status: {hold.Status}");

                var wallet = await _context.Wallets.FirstAsync(w => w.Id == hold.WalletId);
                var forfeitCashKey = $"forfeit:{hold.Id}:cash";
                var forfeitMeritKey = $"forfeit:{hold.Id}:merit";

                // No balance change (amount = 0), but record forfeit in ledger
                if (hold.CashAmount > 0)
                    await ApplyAsync(wallet, WalletTransactionType.ReservationForfeit, PointType.Cash, 0, forfeitCashKey, "Reservation", hold.Id, $"Reservation forfeited for auction {hold.AuctionId}");
                if (hold.MeritAmount > 0)
                    await ApplyAsync(wallet, WalletTransactionType.ReservationForfeit, PointType.Merit, 0, forfeitMeritKey, "Reservation", hold.Id, $"Reservation forfeited for auction {hold.AuctionId}", meritRate: hold.MeritRate);

                hold.Status = ReservationHoldStatus.Forfeited;
                hold.ResolvedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            });
        }
        public async Task GrantMeritAsync(Guid walletId, decimal meritAmount, string source, Guid? referenceId = null)
        {
            await WithRetryAsync(async () =>
            {
                var wallet = await _context.Wallets.FirstAsync(w => w.Id == walletId);
                if (wallet.IsLocked) throw new InvalidOperationException("Wallet is locked.");

                // Deterministic idempotency key based on source
                string idempotencyKey;
                if (source.StartsWith("referral:"))
                {
                    // source = "referral:{inviteeId}"
                    idempotencyKey = source;
                }
                else if (source.StartsWith("share:"))
                {
                    // source = "share:{walletId}:{contentId}"
                    idempotencyKey = source;
                }
                else if (source.StartsWith("cashback:"))
                {
                    // source = "cashback:{auctionId}:{walletId}"
                    idempotencyKey = source;
                }
                else
                {
                    idempotencyKey = $"merit:{source}:{Guid.NewGuid():N}";
                }

                await ApplyAsync(wallet, WalletTransactionType.MeritEarned, PointType.Merit, meritAmount, idempotencyKey, source, referenceId, $"Merit granted: {source}", meritRate: WalletRatio.MeritPerCashPoint);
                await _context.SaveChangesAsync();
            });
        }
        private async Task WithRetryAsync(Func<Task> action, int attempts = 3)
        {
            for (int i = 0; i < attempts; i++)
            {
                try { await action(); return; }
                catch (DbUpdateConcurrencyException)
                {
                    if (i == attempts - 1) throw;
                    await Task.Delay(50 * (i + 1));
                }
            }
        }
        public async Task<WalletTopUp?> CreditTopUpAsync(Guid topUpId, decimal paidAmount, string invoiceId, CancellationToken ct = default)
        {
            for (var attempt = 1; ; attempt++)
            {
                var topUp = await _context.WalletTopUps.FirstAsync(t => t.Id == topUpId, ct);

                // Already credited by another path (webhook, earlier verify, sweeper)
                if (topUp.Status == TopUpStatus.Paid) return null;

                // Safety check: what Xendit says was paid must match what we asked for
                if (paidAmount != topUp.Amount)
                    throw new InvalidOperationException(
                        $"Top-up {topUp.Id}: paid {paidAmount} but expected {topUp.Amount}");

                var wallet = await _context.Wallets.FirstAsync(w => w.Id == topUp.WalletId, ct);

                // 1. Add the ledger row and update the balance (in memory, nothing saved yet)
                var tx = await ApplyAsync(
                    wallet,
                    WalletTransactionType.TopUp,
                    PointType.Cash,
                    topUp.Amount,
                    idempotencyKey: $"topup:{invoiceId}",
                    referenceType: "TopUp",
                    referenceId: topUp.Id,
                    description: "Xendit top-up",
                    ct: ct);

                // 2. Mark the top-up paid
                topUp.Status = TopUpStatus.Paid;
                topUp.PaidAt = DateTime.UtcNow;

                try
                {
                    // 3. One SaveChanges = ledger row + wallet balance + top-up status together
                    await _context.SaveChangesAsync(ct);
                    return topUp;   // true: this call credited. false: key already existed
                }
                catch (DbUpdateConcurrencyException) when (attempt < 3)
                {
                    // Someone else changed the wallet at the same moment (Wallet.RowVersion). Retry from fresh data.
                    _context.ChangeTracker.Clear();
                }
            }
        }
        public async Task<TopUpCompleteResponseDTO?> VerifyTopUpAsync(int userId, Guid topUpId, CancellationToken ct = default)
        {
            var topUp = await _context.WalletTopUps
                .Join(_context.Wallets, t => t.WalletId, w => w.Id, (t, w) => new { t, w })
                .Where(x => x.t.Id == topUpId && x.w.UserId == userId)   // ownership check
                .Select(x => x.t)
                .FirstOrDefaultAsync(ct);
            if (topUp is null) return null;                               // controller returns 404

            var wallet = await _context.Wallets.AsNoTracking().FirstAsync(w => w.Id == topUp.WalletId, ct);
            var defaultTopUpResponse = new TopUpCompleteResponseDTO
            {
                Id = topUp.Id,
                WalletId = topUp.WalletId,
                Amount = topUp.Amount,
                Fee = topUp.Fee,
                XenditReferenceId = topUp.XenditReferenceId,
                XenditPaymentId = topUp.XenditPaymentId,
                PaymentMethod = topUp.PaymentMethod,
                CheckoutUrl = topUp.CheckoutUrl,
                Status = topUp.Status,
                PaidAt = topUp.PaidAt,
                ExpiresAt = topUp.ExpiresAt,
                CreatedAt = topUp.CreatedAt
            };
            if (topUp.Status == TopUpStatus.Paid)
                return defaultTopUpResponse; // already credited, no need to check Xendit

            if (string.IsNullOrEmpty(topUp.XenditPaymentId))
                return defaultTopUpResponse; // no Xendit payment ID, cannot verify

            var invoice = await _xendit.GetInvoiceAsync(topUp.XenditPaymentId, ct);

            switch (invoice.Status)
            {
                case "PAID":
                case "SETTLED":
                    var credited = await CreditTopUpAsync(topUp.Id, invoice.PaidAmount ?? invoice.Amount, invoice.Id, ct);
                    // wallet = await _context.Wallets.AsNoTracking().FirstAsync(w => w.Id == topUp.WalletId, ct);
                    // return new("Paid", credited, wallet.CashPoints);
                    return new TopUpCompleteResponseDTO
                    {
                        Id = credited!.Id,
                        WalletId = credited.WalletId,
                        Amount = credited.Amount,
                        Fee = credited.Fee,
                        XenditReferenceId = credited.XenditReferenceId,
                        XenditPaymentId = credited.XenditPaymentId,
                        PaymentMethod = credited.PaymentMethod,
                        CheckoutUrl = credited.CheckoutUrl,
                        Status = credited != null ? TopUpStatus.Paid : TopUpStatus.Paid, // already credited or just credited
                        PaidAt = credited?.PaidAt ?? credited!.PaidAt,
                        ExpiresAt = credited.ExpiresAt,
                        CreatedAt = credited.CreatedAt
                    };


                case "EXPIRED":
                    if (topUp.Status == TopUpStatus.Pending)
                    {
                        topUp.Status = TopUpStatus.Expired;
                        await _context.SaveChangesAsync(ct);
                    }
                    return new TopUpCompleteResponseDTO
                    {
                        Id = topUp.Id,
                        WalletId = topUp.WalletId,
                        Amount = topUp.Amount,
                        Fee = topUp.Fee,
                        XenditReferenceId = topUp.XenditReferenceId,
                        XenditPaymentId = topUp.XenditPaymentId,
                        PaymentMethod = topUp.PaymentMethod,
                        CheckoutUrl = topUp.CheckoutUrl,
                        Status = topUp.Status,
                        PaidAt = topUp.PaidAt,
                        ExpiresAt = topUp.ExpiresAt,
                        CreatedAt = topUp.CreatedAt
                    };

                default: // PENDING
                    return defaultTopUpResponse;
            }
        }
    }
}