namespace MyStash.CoreApi.Features.Accounts;

internal class User
{
    public int Id { get; set; }
}

internal class UserEmail
{
    public int Id { get; set; }
    public required string Email { get; set; }

    public int UserId { get; set; }    
}

internal class Account
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; }

    public int UserId { get; set; }    
}