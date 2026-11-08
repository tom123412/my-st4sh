using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyStash.CoreApi.Features.Accounts;

internal class UserConfiguration : IEntityTypeConfiguration<User>
{
    void IEntityTypeConfiguration<User>.Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(p => p.Id);
    }
}

internal class UserEmailConfiguration : IEntityTypeConfiguration<UserEmail>
{
    void IEntityTypeConfiguration<UserEmail>.Configure(EntityTypeBuilder<UserEmail> builder)
    {
        builder.ToTable("Emails");
        builder.HasKey(p => p.Id);

        // Enforces the database FK constraint without needing a navigation property
        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

internal class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    void IEntityTypeConfiguration<Account>.Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(p => p.Id);

        // Enforces the database FK constraint without needing a navigation property
        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}