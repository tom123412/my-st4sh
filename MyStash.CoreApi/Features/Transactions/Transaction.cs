namespace MyStash.CoreApi.Features.Transactions;

internal class CategoryType
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

internal class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; }

    public int CategoryTypeId { get; set; }
}

internal class Transaction
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public bool IsExepmt { get; set; }
    public decimal Balance { get; private set; }

    public int CategoryId { get; set; }
    public int AccountId { get; set; }

    public long RowIndex { get; private set; }
}