using System.Security.Claims;
using biddingServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using biddingServer.services.wallet;
using RequestDTO = DTO.Request.Wallet;
using ResponseDTO = DTO.Response.Wallet;

namespace biddingServer.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        private int GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userId, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseDTO.WalletResponseDTO>> Wallet()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var wallet = await _walletService.GetWalletAsync(userId);
            if (wallet == null) return NotFound("Wallet not found.");

            return Ok(wallet);
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<List<ResponseDTO.WalletTransactionResponseDTO>>> Transactions(
            [FromQuery] DateTime? before = null,
            [FromQuery] PointType? pointType = null,
            [FromQuery] int limit = 20)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var transactions = await _walletService.GetTransactionsAsync(userId, before, pointType, limit);
            return Ok(transactions);
        }

        [HttpPost("topup")]
        public async Task<ActionResult<ResponseDTO.TopUpResponseDTO>> TopUp([FromBody] RequestDTO.TopUpRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var topUp = await _walletService.CreateTopUpAsync(userId, dto);
                return Ok(topUp);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("withdraw")]
        public async Task<ActionResult<ResponseDTO.WithdrawalResponseDTO>> Withdraw([FromBody] RequestDTO.WithdrawalRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var withdrawal = await _walletService.CreateWithdrawalAsync(userId, dto);
                return Ok(withdrawal);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("can-afford")]
        public async Task<ActionResult<ResponseDTO.WalletBalanceCheckDTO>> CanAfford(
            [FromQuery] decimal requiredCash,
            [FromQuery] decimal requiredMerit = 0)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var check = await _walletService.CanAffordAsync(userId, requiredCash, requiredMerit);
            return Ok(check);
        }
    }
}
