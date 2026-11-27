using System.IO;
using System.Text.Json;
using biddingServer.Models;
using biddingServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using biddingServer.services.wallet;

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
        public async Task<IActionResult> PaymentWebhook()
        {
            // Verify callback token
            var callbackToken = Request.Headers["x-callback-token"].FirstOrDefault();
            var expectedToken = _config["Xendit:CallbackToken"];
            if (string.IsNullOrEmpty(expectedToken) || callbackToken != expectedToken)
            {
                return Unauthorized("Invalid callback token");
            }

            // Parse webhook payload
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Xendit invoice webhook fields (TODO: confirm exact field names)
            var externalId = root.GetProperty("external_id").GetString();
            var status = root.GetProperty("status").GetString();
            var paidAmount = root.GetProperty("paid_amount").GetDecimal();
            var paymentId = root.GetProperty("payment_id").GetString(); // Xendit payment ID

            if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(status))
            {
                return Ok(); // malformed but return 200 per spec
            }

            // Find top-up by external_id (our XenditReferenceId)
            var topUp = await _context.WalletTopUps
                .FirstOrDefaultAsync(t => t.XenditReferenceId == externalId);

            if (topUp == null)
            {
                return Ok();
            }

            // Already processed
            if (topUp.Status == TopUpStatus.Paid)
            {
                return Ok();
            }

            if (status == "PAID")
            {
                // Verify amount matches
                if (paidAmount != topUp.Amount)
                {
                    return Ok();
                }

                // Get wallet
                var wallet = await _context.Wallets.FirstAsync(w => w.Id == topUp.WalletId);
                var idempotencyKey = $"topup:{paymentId}";
                
                await _walletService.ApplyAsync(
                    wallet,
                    WalletTransactionType.TopUp,
                    PointType.Cash,
                    paidAmount,
                    idempotencyKey,
                    "TopUp",
                    topUp.Id,
                    $"Xendit top-up {externalId}"
                );

                topUp.Status = TopUpStatus.Paid;
                topUp.PaidAt = DateTime.UtcNow;
                topUp.XenditPaymentId = paymentId;

                await _context.SaveChangesAsync();
                return Ok();
            }
            else if (status == "EXPIRED")
            {
                topUp.Status = TopUpStatus.Expired;
                await _context.SaveChangesAsync();
                return Ok();
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