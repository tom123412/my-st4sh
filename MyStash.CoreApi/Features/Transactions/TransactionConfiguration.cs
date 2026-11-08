using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyStash.CoreApi.Features.Accounts;

namespace MyStash.CoreApi.Features.Transactions;

internal class CategoryTypeConfiguration : IEntityTypeConfiguration<CategoryType>
{
    void IEntityTypeConfiguration<CategoryType>.Configure(EntityTypeBuilder<CategoryType> builder)
    {
        builder.HasKey(p => p.Id);
    }
}

internal class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    void IEntityTypeConfiguration<Category>.Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(p => p.Id);

        // Enforces the database FK constraint without needing a navigation property
        builder.HasOne<CategoryType>()
               .WithMany()
               .HasForeignKey(p => p.CategoryTypeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

internal class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    void IEntityTypeConfiguration<Transaction>.Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.RowIndex)
               .ValueGeneratedOnAddOrUpdate()
               ;

        builder.Property(p => p.Balance)
               .ValueGeneratedOnAddOrUpdate()
               ;

        // Enforces the database FK constraint without needing a navigation property
        builder.HasOne<Category>()
               .WithMany()
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        // Enforces the database FK constraint without needing a navigation property
        builder.HasOne<Account>()
               .WithMany()
               .HasForeignKey(p => p.AccountId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}