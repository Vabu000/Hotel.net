using Grpc.Core;
using HotelCatalog.Api.Grpc;
using HotelCatalog.Bll.Interfaces;
using HotelCatalog.Bll.DTOs;
using DomainService = HotelCatalog.Domain.Entities.Service;  

namespace HotelCatalog.Api.Grpc;

public class GrpcCatalogService : CatalogService.CatalogServiceBase
{
    private readonly IServiceService _serviceService;
    private readonly IRoomCategoryService _roomCategoryService;
    private readonly ILogger<GrpcCatalogService> _logger;

    public GrpcCatalogService(
        IServiceService serviceService,
        IRoomCategoryService roomCategoryService,
        ILogger<GrpcCatalogService> logger)
    {
        _serviceService = serviceService;
        _roomCategoryService = roomCategoryService;
        _logger = logger;
    }

    public override async Task<GetAllServicesResponse> GetAllServices(
        GetAllServicesRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetAllServices: Page={Page}", request.Page);

        var services = await _serviceService.GetAllAsync(context.CancellationToken);

        var response = new GetAllServicesResponse
        {
            TotalCount = services.Count()
        };

        response.Items.AddRange(services.Select(MapServiceToGrpc));

        return response;
    }

    public override async Task<ServiceResponse> GetServiceById(
        GetServiceByIdRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetServiceById: Id={ServiceId}", request.Id);

        var service = await _serviceService.GetByIdAsync(request.Id, context.CancellationToken);

        if (service == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Service with ID '{request.Id}' not found"));
        }

        return new ServiceResponse
        {
            Service = MapServiceToGrpc(service)
        };
    }

    public override async Task<RoomCategoryResponse> GetRoomCategoryById(
        GetRoomCategoryByIdRequest request,
        ServerCallContext context)
    {
        var category = await _roomCategoryService.GetByIdAsync(request.Id, context.CancellationToken);
        if (category is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Room category with ID '{request.Id}' not found"));
        }

        return new RoomCategoryResponse
        {
            Category = new RoomCategoryMessage
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description ?? string.Empty
            }
        };
    }

    public override async Task<ServiceResponse> CreateService(
        CreateServiceRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC CreateService: Name={Name}", request.Name);

        var dto = new CreateServiceDto
        {
            Name = request.Name,
            Description = request.Description,
            Price = (decimal)request.Price
        };

        var created = await _serviceService.CreateAsync(dto, context.CancellationToken);

        return new ServiceResponse
        {
            Service = MapServiceToGrpc(created)
        };
    }

    private static ServiceMessage MapServiceToGrpc(ServiceDto dto)
    {
        return new ServiceMessage
        {
            ServiceId = dto.ServiceId,
            Name = dto.Name ?? string.Empty,
            Description = dto.Description ?? string.Empty,
            Price = (double)dto.Price
        };
    }
}
