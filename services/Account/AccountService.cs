using Azure;
using DTO.Response.Account;
using Microsoft.EntityFrameworkCore;
using ResponseDTO = DTO.Response.Account;
using biddingServer.Models;
using RequestBasicInfo = DTO.Request.Account.BasicInfoDTO;

namespace biddingServer.services.account
{
    public interface IAccountService
    {
        Task<BasicInfoDTO> GetBasicInfo(string IdentityEmail = "");
        Task<BasicInfoDTO> CreateBasicInfo(RequestBasicInfo account);
        Task<BasicInfoDTO> UpdateBasicInfo(RequestBasicInfo account);
    }

    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _context; // DbContext or repository
        public AccountService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<BasicInfoDTO> GetBasicInfo(string IdentityEmail = "")
        {
            if (IdentityEmail == "") return new BasicInfoDTO();


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
        public async Task<BasicInfoDTO> UpdateBasicInfo(RequestBasicInfo account)
        {
            var accountId = account.id;

            var totalAccounts = await _context.Accounts.CountAsync();

            var accountExists = await _context.Accounts
                .AnyAsync(a => a.Id == accountId);

            Console.WriteLine($"Received ID: {accountId}");
            Console.WriteLine($"Total accounts: {totalAccounts}");
            Console.WriteLine($"Account exists: {accountExists}");

            var existingAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId);

            existingAccount.UserName = account.userName;
            existingAccount.FullName = account.fullName;
            existingAccount.Email = account.email;
            existingAccount.DateOfBirth = account.dateOfBirth;
            existingAccount.Role = account.role;
            existingAccount.Gender = account.gender;

            await _context.SaveChangesAsync();

            return new BasicInfoDTO
            {
                Id = existingAccount.Id,
                UserName = existingAccount.UserName,
                FullName = existingAccount.FullName,
                Email = existingAccount.Email,
                DateOfBirth = existingAccount.DateOfBirth,
                Role = existingAccount.Role,
                Gender = existingAccount.Gender
            };
        }
        public async Task<BasicInfoDTO> CreateBasicInfo(RequestBasicInfo account)
        {
            var newAccount = new AccountModel
            {
                Id = account.id,
                UserName = account.userName,
                FullName = account.fullName,
                Email = account.email,
                DateOfBirth = account.dateOfBirth,
                Role = account.role,
                Gender = account.gender
            };

            await _context.Accounts.AddAsync(newAccount);
            await _context.SaveChangesAsync();

            return new BasicInfoDTO
            {
                Id = newAccount.Id,
                UserName = newAccount.UserName,
                FullName = newAccount.FullName,
                Email = newAccount.Email,
                DateOfBirth = newAccount.DateOfBirth,
                Role = newAccount.Role,
                Gender = newAccount.Gender
            };
        }
    }
}