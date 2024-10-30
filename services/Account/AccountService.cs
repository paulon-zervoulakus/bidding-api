using Azure;
using DTO.Response.Account;
using Microsoft.EntityFrameworkCore;
using ResponseDTO = DTO.Response.Account;

namespace biddingServer.services.account
{
    public interface IAccountService
    {
        Task<ResponseDTO.BasicInfoDTO> GetBasicInfo(string IdentityEmail = "");
    }

    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _context; // DbContext or repository
        public AccountService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ResponseDTO.BasicInfoDTO> GetBasicInfo(string IdentityEmail = "")
        {
            if (IdentityEmail == "") return new ResponseDTO.BasicInfoDTO();


            var account = await _context.Accounts
            .Select(x => new BasicInfoDTO
            {
                Id = x.Id,
                UserName = x.UserName,
                Email = x.Email,
                FullName = x.FullName,
                DateOfBirth = x.DateOfBirth,
                Role = x.Role,
                Gender = x.Gender
            })
            .Where(x => x.Email == IdentityEmail).FirstOrDefaultAsync();
            return account ?? new ResponseDTO.BasicInfoDTO();

        }
    }

}