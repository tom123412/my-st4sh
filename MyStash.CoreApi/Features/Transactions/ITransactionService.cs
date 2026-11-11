namespace MyStash.CoreApi.Features.Transactions;

public interface ITransactionService
{
    IQueryable<TransactionResponse> GetAll(
        string emailAddress,
        CancellationToken cancellationToken);

    Task<TransactionResponse?> GetByIdAsync(
        int id,
        string emailAddress,
        CancellationToken cancellationToken);

    Task<TransactionResponse?> CreateAsync(
        CreateTransactionRequest request,
        string emailAddress,
        CancellationToken cancellationToken);
}
