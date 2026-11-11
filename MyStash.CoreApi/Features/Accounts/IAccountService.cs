namespace MyStash.CoreApi.Features.Accounts;

public interface IAccountService
{
    IQueryable<AccountResponse> GetAll(string emailAddress);
}
