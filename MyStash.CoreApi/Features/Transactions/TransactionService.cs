using Microsoft.EntityFrameworkCore;
using MyStash.CoreApi.Data;
using MyStash.CoreApi.Features.Accounts;

namespace MyStash.CoreApi.Features.Transactions;

internal sealed class TransactionService(MyStashDbContext dbContext) : ITransactionService
{
    public async Task<IReadOnlyList<TransactionResponse>> GetAllAsync(
        string emailAddress,
        CancellationToken cancellationToken)
    {
        return await (
            from transaction in dbContext.Set<Transaction>().AsNoTracking()
            join account in dbContext.Set<Account>() on transaction.AccountId equals account.Id
            join category in dbContext.Set<Category>() on transaction.CategoryId equals category.Id
            join categoryType in dbContext.Set<CategoryType>() on category.CategoryTypeId equals categoryType.Id
            where dbContext.Set<UserEmail>().Any(userEmail =>
                userEmail.UserId == account.UserId && userEmail.Email == emailAddress)
            select new TransactionResponse(
                transaction.Id,
                transaction.Date,
                transaction.Amount,
                transaction.IsExepmt,
                transaction.Balance,
                category.Name,
                categoryType.Name,
                transaction.AccountId))
            .ToListAsync(cancellationToken);
    }
}
