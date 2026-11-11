using System.ComponentModel.DataAnnotations;

namespace MyStash.CoreApi.Features.Transactions;

/// <summary>Payload for creating a transaction on an account the user can access.</summary>
public sealed record CreateTransactionRequest
{
    public required DateTimeOffset Date { get; init; }

    [Range(typeof(decimal), "-9999999999999999.99", "9999999999999999.99")]
    public required decimal Amount { get; init; }

    public required bool IsExempt { get; init; }

    [Range(1, int.MaxValue)]
    public required int CategoryId { get; init; }

    [Range(1, int.MaxValue)]
    public required int AccountId { get; init; }
}
