using HotelCatalog.Dal.Data;
using HotelCatalog.Domain.Entities;
using HotelPlatform.Aggregator.DTOs;
using HotelPlatform.Aggregator.Services;
using HotelPlatform.ServiceDefaults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HotelReservation.Tests;

public class CatalogAndProblemDetailsTests
{
    [Fact]
    public void RoomCategoryDetailsUsesItsCategoryKeyAsUniqueForeignKey()
    {
        var options = new DbContextOptionsBuilder<HotelCatalogDbContext>()
            .UseNpgsql("Host=localhost;Database=CatalogModelTests")
            .Options;
        using var context = new HotelCatalogDbContext(options);

        var details = context.Model.FindEntityType(typeof(RoomCategoryDetails));
        Assert.NotNull(details);

        var relationship = Assert.Single(details.GetForeignKeys());
        Assert.True(relationship.IsUnique);
        Assert.Equal(typeof(RoomCategory), relationship.PrincipalEntityType.ClrType);
        Assert.Equal(
            details.FindPrimaryKey()!.Properties.Select(property => property.Name),
            relationship.Properties.Select(property => property.Name));
        Assert.Equal("roomcategorydetails", details.GetTableName());
        Assert.Contains("20261113130635_AddRoomCategoryDetails", context.Database.GetMigrations());

        var roomServices = context.Model.GetEntityTypes()
            .Single(entity => entity.GetTableName() == "roomservices");
        Assert.NotNull(roomServices.FindProperty("IsDeleted"));
        Assert.True(roomServices.FindProperty("Version")!.IsConcurrencyToken);
        Assert.Equal(10, roomServices.FindProperty(nameof(RoomService.AdditionalPrice))!.GetPrecision());
        Assert.Equal(2, roomServices.FindProperty(nameof(RoomService.AdditionalPrice))!.GetScale());
        var roomServicesDesignTime = context.GetService<IDesignTimeModel>().Model
            .GetEntityTypes()
            .Single(entity => entity.GetTableName() == "roomservices");
        Assert.Contains(roomServicesDesignTime.GetCheckConstraints(), constraint =>
            constraint.Name == "ck_roomservices_additionalprice_nonnegative");

        var category = context.Model.FindEntityType(typeof(RoomCategory));
        Assert.NotNull(category);
        Assert.True(category.FindProperty("IsDeleted")!.GetDefaultValue() is false);
        Assert.True(category.FindProperty("Version")!.IsConcurrencyToken);
        Assert.Contains("20261113161500_AddCatalogAuditFields", context.Database.GetMigrations());
        var migrationIds = context.Database.GetMigrations().ToList();
        Assert.True(migrationIds.IndexOf("20261113161500_AddCatalogAuditFields") <
                    migrationIds.IndexOf("20261113170000_BackfillCatalogSeedVersions"));
        Assert.True(migrationIds.IndexOf("20261113170000_BackfillCatalogSeedVersions") <
                    migrationIds.IndexOf("20261113180000_AddRoomServiceAdditionalPrice"));
    }

    [Fact]
    public void ErrorResultIsConvertedToProblemDetailsWithTraceInformation()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/roomcategories/42";
        context.TraceIdentifier = "trace-123";
        var result = new BadRequestObjectResult("Invalid room category");

        var problem = ProblemDetailsResultFilter.CreateProblemDetails(result, context);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Equal("Invalid room category", problem.Detail);
        Assert.Equal("/api/roomcategories/42", problem.Instance);
        Assert.Equal("trace-123", problem.Extensions["traceId"]);
    }

    [Fact]
    public void ModelStateErrorsArePreservedAsValidationProblemDetails()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Name", "Name is required");
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-456";
        var result = new BadRequestObjectResult(modelState);

        var problem = ProblemDetailsResultFilter.CreateProblemDetails(result, context);

        var validationProblem = Assert.IsType<ValidationProblemDetails>(problem);
        Assert.Equal(new[] { "Name is required" }, validationProblem.Errors["Name"]);
        Assert.Equal("trace-456", validationProblem.Extensions["traceId"]);
    }

    [Fact]
    public void AggregatorMarksUnavailableSectionsAndUnknownAvailability()
    {
        var result = RoomDetailsAssembler.Assemble(
            new RoomDetailsDto { RoomId = 7, RoomNumber = "707", Capacity = 2, Price = 100m },
            new AggregationDependencyResult<RoomCategoryInfo>(null, IsAvailable: false),
            new AggregationDependencyResult<List<ReviewInfo>>(null, IsAvailable: false),
            new AggregationDependencyResult<List<ReservationInfo>>(null, IsAvailable: false));

        Assert.True(result.IsPartial);
        Assert.False(result.AvailabilityKnown);
        Assert.False(result.IsAvailable);
        Assert.Equal(new[] { "category", "reviews", "reservations" }, result.UnavailableSections);
        Assert.Empty(result.Reviews);
        Assert.Empty(result.Reservations);
    }

    [Fact]
    public void AggregatorDoesNotReportRoomAvailableWhenReservationIsConfirmed()
    {
        var result = RoomDetailsAssembler.Assemble(
            new RoomDetailsDto { RoomId = 7, RoomNumber = "707", Capacity = 2, Price = 100m },
            new AggregationDependencyResult<RoomCategoryInfo>(new RoomCategoryInfo(), IsAvailable: true),
            new AggregationDependencyResult<List<ReviewInfo>>(new List<ReviewInfo>(), IsAvailable: true),
            new AggregationDependencyResult<List<ReservationInfo>>(
                new List<ReservationInfo> { new() { Status = "Confirmed" } },
                IsAvailable: true));

        Assert.False(result.IsPartial);
        Assert.True(result.AvailabilityKnown);
        Assert.False(result.IsAvailable);
    }
}
