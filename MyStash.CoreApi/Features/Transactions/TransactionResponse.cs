namespace MyStash.CoreApi.Features.Transactions;

/// <summary>Represents a transaction returned by the API.</summary>
public sealed record TransactionResponse(
    int Id,
    DateTime Date,
    decimal Amount,
    bool IsExempt,
    decimal Balance,
    string CategoryName,
    string CategoryTypeName,
    int AccountId);
