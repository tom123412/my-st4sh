namespace MyStash.CoreApi.Features.Transactions;

public interface ITransactionService
{
    IQueryable<TransactionResponse> GetAll(string emailAddress);
}
