using biddingServer.Models;
namespace TestData
{
    public class AccountData
    {
        public List<AccountModel> Accounts { get; set; } = new List<AccountModel>
        {
            new AccountModel
            {
                Id = 1,
                UserName = "johndoe",
                Email = "test@gmail.com",
                Password = "test",
                FullName = "John Doe",
                DateOfBirth = new DateTime(1990, 5, 23),
                CreatedDate = DateTime.Now.AddYears(-5),
                UpdatedDate = DateTime.Now.AddMonths(-1),
                LastLoggedIn = DateTime.Now.AddHours(-2),
                IsLoggedIn = true,
                Role = RoleEnumerated.Member,
                Gender = GenderEnumerated.Male
            },
            new AccountModel
            {
                Id = 2,
                UserName = "maria",
                Email = "mari@gmail.com",
                Password = "test",
                FullName = "Mari ang Palad",
                DateOfBirth = new DateTime(1985, 8, 15),
                CreatedDate = DateTime.Now.AddYears(-3),
                UpdatedDate = DateTime.Now.AddDays(-10),
                LastLoggedIn = DateTime.Now.AddHours(-5),
                IsLoggedIn = false,
                Role = RoleEnumerated.Administrator,
                Gender = GenderEnumerated.Female
            }
        };
    }
}
