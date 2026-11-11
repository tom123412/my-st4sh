using Microsoft.EntityFrameworkCore;
using MyStash.CoreApi.Data;

namespace MyStash.CoreApi.Features.Accounts;

internal sealed class AccountService(MyStashDbContext dbContext) : IAccountService
{
    public IQueryable<AccountResponse> GetAll(string emailAddress)
    {
        return dbContext.Set<Account>()
            .AsNoTracking()
            .Where(account => dbContext.Set<UserEmail>().Any(userEmail =>
                userEmail.UserId == account.UserId && userEmail.Email == emailAddress))
            .Select(account => new AccountResponse(
                account.Id,
                account.Name,
                account.IsActive));
    }
}
