using HotelCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelCatalog.Dal.Configurations;

public class RoomCategoryDetailsConfiguration : IEntityTypeConfiguration<RoomCategoryDetails>
{
    public void Configure(EntityTypeBuilder<RoomCategoryDetails> builder)
    {
        builder.ToTable("roomcategorydetails");
        builder.HasKey(details => details.CategoryId);

        builder.Property(details => details.CategoryId).HasColumnName("categoryid");
        builder.Property(details => details.MaxOccupancy)
            .HasColumnName("maxoccupancy")
            .IsRequired();
        builder.Property(details => details.AreaSquareMeters)
            .HasColumnName("areasquaremeters")
            .HasPrecision(7, 2)
            .IsRequired();

        builder.HasOne(details => details.RoomCategory)
            .WithOne(category => category.Details)
            .HasForeignKey<RoomCategoryDetails>(details => details.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new RoomCategoryDetails { CategoryId = 1, MaxOccupancy = 2, AreaSquareMeters = 20.00m },
            new RoomCategoryDetails { CategoryId = 2, MaxOccupancy = 3, AreaSquareMeters = 32.50m },
            new RoomCategoryDetails { CategoryId = 3, MaxOccupancy = 4, AreaSquareMeters = 55.00m });
    }
}
