namespace MyStash.CoreApi.Features.Accounts;

/// <summary>Represents an account available to the authenticated user.</summary>
public sealed record AccountResponse(
    int Id,
    string Name,
    bool IsActive);
