using Microsoft.EntityFrameworkCore;
using MyStash.CoreApi.Data;

namespace MyStash.CoreApi.Features.Transactions;

internal sealed class TransactionService(MyStashDbContext dbContext) : ITransactionService
{
    public async Task<IReadOnlyList<TransactionResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await (
            from transaction in dbContext.Set<Transaction>().AsNoTracking()
            join category in dbContext.Set<Category>() on transaction.CategoryId equals category.Id
            join categoryType in dbContext.Set<CategoryType>() on category.CategoryTypeId equals categoryType.Id
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
