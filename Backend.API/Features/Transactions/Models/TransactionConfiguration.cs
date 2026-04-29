using Backend.Features.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Transactions.Models;


public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.TransactionId);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Description)
            .IsRequired();

<<<<<<< feat/41-create-transaction-endpoint
         builder.Property(t => t.TransactionType)
             .IsRequired()
             .HasColumnType("transaction_type");
=======
        builder.Property(t => t.TransactionType)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("transaction_type");
>>>>>>> main

        builder.Property(t => t.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();
    }
}