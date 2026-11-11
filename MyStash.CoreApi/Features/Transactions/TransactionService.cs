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
            from transaction in GetAccessibleTransactions(emailAddress).AsNoTracking()
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
                transaction.AccountId);
    }

    public Task<TransactionResponse?> GetByIdAsync(
        int id,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        return GetAll(emailAddress, cancellationToken)
            .SingleOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);
    }

    public async Task<TransactionResponse?> CreateAsync(
        CreateTransactionRequest request,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        var canUseAccount = await GetAccessibleAccounts(emailAddress)
            .Where(account => account.Id == request.AccountId)
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

    public async Task<bool> DeleteAsync(
        int id,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        var transaction = await GetAccessibleTransactions(emailAddress)
            .Where(item => item.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        if (transaction is null)
            return false;

        dbContext.Set<Transaction>().Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Account> GetAccessibleAccounts(string emailAddress)
    {
        return dbContext.Set<Account>()
            .Where(account => dbContext.Set<UserEmail>().Any(userEmail =>
                userEmail.UserId == account.UserId && userEmail.Email == emailAddress));
    }

    private IQueryable<Transaction> GetAccessibleTransactions(string emailAddress)
    {
        return
            from transaction in dbContext.Set<Transaction>()
            join account in GetAccessibleAccounts(emailAddress)
                on transaction.AccountId equals account.Id
            select transaction;
    }
}
