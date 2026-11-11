using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace MyStash.CoreApi.Features.Accounts;

internal static class AccountODataModel
{
    public static IEdmModel GetModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<AccountResponse>("Accounts");
        return builder.GetEdmModel();
    }
}
