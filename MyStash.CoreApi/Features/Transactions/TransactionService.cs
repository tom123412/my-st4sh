using Microsoft.EntityFrameworkCore;
using MyStash.CoreApi.Data;
using MyStash.CoreApi.Features.Accounts;

namespace MyStash.CoreApi.Features.Transactions;

internal sealed class TransactionService(MyStashDbContext dbContext) : ITransactionService
{
    public IQueryable<TransactionResponse> GetAll(
        string emailAddress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return
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
                transaction.AccountId);
    }

    public Task<TransactionResponse?> GetByIdAsync(
        int id,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        return GetAll(emailAddress, cancellationToken)
            .FirstOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);
    }

    public async Task<TransactionResponse?> CreateAsync(
        CreateTransactionRequest request,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        var canUseAccount = await (
            from account in dbContext.Set<Account>()
            join userEmail in dbContext.Set<UserEmail>() on account.UserId equals userEmail.UserId
            where account.Id == request.AccountId && userEmail.Email == emailAddress
            select account.Id)
            .AnyAsync(cancellationToken);

        var categoryIsActive = await dbContext.Set<Category>()
            .AnyAsync(category =>
                category.Id == request.CategoryId && category.IsActive,
                cancellationToken);

        if (!canUseAccount || !categoryIsActive)
            return null;

        var transaction = new Transaction
        {
            Date = request.Date.UtcDateTime,
            Amount = request.Amount,
            IsExepmt = request.IsExempt,
            CategoryId = request.CategoryId,
            AccountId = request.AccountId
        };

        dbContext.Set<Transaction>().Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(transaction.Id, emailAddress, cancellationToken);
    }

}
