using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace MyStash.CoreApi.Features.Transactions;

internal static class TransactionODataModel
{
    public static IEdmModel GetModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<TransactionResponse>("Transactions");
        return builder.GetEdmModel();
    }
}
