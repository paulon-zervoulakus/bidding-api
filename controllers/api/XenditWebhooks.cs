using System.IO;
using System.Text.Json;
using biddingServer.Models;
using biddingServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using biddingServer.services.wallet;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;

namespace biddingServer.Controllers.api
{
    [ApiController]
    [Route("api/webhooks/xendit")]
    [AllowAnonymous]
    public class XenditWebhooksController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public XenditWebhooksController(IWalletService walletService, ApplicationDbContext context, IConfiguration config)
        {
            _walletService = walletService;
            _context = context;
            _config = config;
        }

        [HttpPost("payment")]
        public async Task<IActionResult> PaymentWebhook(CancellationToken ct)
        {
            var received = Request.Headers["x-callback-token"].ToString();
            var expected = _config["Xendit:CallbackToken"];
            if (string.IsNullOrEmpty(expected) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(received)))
                return Unauthorized();

            using var reader = new StreamReader(Request.Body);
            using var doc = JsonDocument.Parse(await reader.ReadToEndAsync(ct));
            var root = doc.RootElement;

            string? Str(string name) =>
                root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

            var invoiceId = Str("id");              // invoice id, same value stored in XenditPaymentId
            var externalId = Str("external_id");     // our XenditReferenceId
            var status = Str("status");
            decimal? paidAmount =
                root.TryGetProperty("paid_amount", out var pa) && pa.ValueKind == JsonValueKind.Number
                    ? pa.GetDecimal() : null;

            if (string.IsNullOrEmpty(invoiceId) || string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(status))
                return Ok();   // malformed or test payload

            var topUp = await _context.WalletTopUps
                .FirstOrDefaultAsync(t => t.XenditReferenceId == externalId, ct);
            if (topUp is null || topUp.Status == TopUpStatus.Paid) return Ok();

            if (status is "PAID" or "SETTLED")
            {
                if (paidAmount != topUp.Amount)
                {
                    Console.WriteLine("Top-up {Id}: paid {Paid} expected {Expected}", topUp.Id, paidAmount, topUp.Amount);
                    return Ok();   // leave Pending for manual review
                }
                await _walletService.CreditTopUpAsync(topUp.Id, paidAmount.Value, invoiceId, ct);
            }
            else if (status == "EXPIRED" && topUp.Status == TopUpStatus.Pending)
            {
                topUp.Status = TopUpStatus.Expired;
                await _context.SaveChangesAsync(ct);
            }

            return Ok();
        }

        [HttpPost("payout")]
        public async Task<IActionResult> PayoutWebhook()
        {
            // Verify callback token
            var callbackToken = Request.Headers["x-callback-token"].FirstOrDefault();
            var expectedToken = _config["Xendit:CallbackToken"];
            if (string.IsNullOrEmpty(expectedToken) || callbackToken != expectedToken)
            {
                return Unauthorized("Invalid callback token");
            }

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Xendit payout webhook fields (TODO: confirm exact field names)
            var externalId = root.GetProperty("external_id").GetString();
            var status = root.GetProperty("status").GetString();
            var failureReason = root.TryGetProperty("failure_reason", out var fr) ? fr.GetString() : null;
            var payoutId = root.GetProperty("payout_id").GetString();

            if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(status))
            {
                return Ok();
            }

            var withdrawal = await _context.WalletWithdrawals
                .FirstOrDefaultAsync(w => w.XenditReferenceId == externalId);

            if (withdrawal == null)
            {
                return Ok();
            }

            // Already processed
            if (withdrawal.Status == WithdrawalStatus.Completed || withdrawal.Status == WithdrawalStatus.Failed)
            {
                return Ok();
            }

            if (status == "COMPLETED")
            {
                withdrawal.Status = WithdrawalStatus.Completed;
                withdrawal.ProcessedAt = DateTime.UtcNow;
                withdrawal.XenditPayoutId = payoutId;

                // Update original withdrawal transaction to Completed
                var tx = await _context.WalletTransactions
                    .FirstOrDefaultAsync(t => t.ReferenceType == "Withdrawal" && t.ReferenceId == withdrawal.Id);
                if (tx != null)
                {
                    tx.Status = TransactionStatus.Completed;
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            else if (status == "FAILED")
            {
                withdrawal.Status = WithdrawalStatus.Failed;
                withdrawal.FailureReason = failureReason;
                withdrawal.ProcessedAt = DateTime.UtcNow;
                withdrawal.XenditPayoutId = payoutId;

                // Refund cash points via WithdrawalRefund
                var wallet = await _context.Wallets.FirstAsync(w => w.Id == withdrawal.WalletId);
                var refundKey = $"withdrawal_refund:{withdrawal.Id}";
                await _walletService.ApplyAsync(
                    wallet,
                    WalletTransactionType.WithdrawalRefund,
                    PointType.Cash,
                    withdrawal.Amount, // positive = credit back
                    refundKey,
                    "WithdrawalRefund",
                    withdrawal.Id,
                    $"Refund for failed withdrawal {externalId}"
                );

                // Update original withdrawal transaction to Reversed
                var origTx = await _context.WalletTransactions
                    .FirstOrDefaultAsync(t => t.ReferenceType == "Withdrawal" && t.ReferenceId == withdrawal.Id);
                if (origTx != null)
                {
                    origTx.Status = TransactionStatus.Reversed;
                }

                await _context.SaveChangesAsync();
                return Ok();
            }

            return Ok();
        }
    }
}