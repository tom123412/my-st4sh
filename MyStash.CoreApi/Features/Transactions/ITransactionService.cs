namespace MyStash.CoreApi.Features.Transactions;

internal interface ITransactionService
{
    Task<IReadOnlyList<TransactionResponse>> GetAllAsync(
        string emailAddress,
        CancellationToken cancellationToken);
}
