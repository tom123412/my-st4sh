namespace MyStash.CoreApi.Features.Transactions;

internal interface ITransactionService
{
    Task<IReadOnlyList<TransactionResponse>> GetAllAsync(CancellationToken cancellationToken);
}
