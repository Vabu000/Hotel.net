using AutoMapper;
using Microsoft.Extensions.Logging;
using HotelCatalog.Bll.DTOs;
using HotelCatalog.Bll.Interfaces;
using HotelCatalog.Dal.Interfaces;
using HotelCatalog.Domain.Entities;
using HotelCatalog.Dal.Specifications;

namespace HotelCatalog.Bll.Services
{
    public class ServiceService : IServiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiceService> _logger;

        public ServiceService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ServiceService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
            return service != null ? _mapper.Map<ServiceDto>(service) : null;
        }

        public async Task<ServiceDto?> GetWithRoomCategoriesAsync(int id, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Services.GetServiceWithRoomCategoriesAsync(id, cancellationToken);
            return service != null ? _mapper.Map<ServiceDto>(service) : null;
        }

        public async Task<IEnumerable<ServiceDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var services = await _unitOfWork.Services.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<ServiceDto>>(services);
        }

        public async Task<IEnumerable<ServiceDto>> GetAllWithRoomCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var services = await _unitOfWork.Services.GetAllServicesWithRoomCategoriesAsync(cancellationToken);
            return _mapper.Map<IEnumerable<ServiceDto>>(services);
        }

        public async Task<IEnumerable<ServiceDto>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default)
        {
            if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            {
                throw new ArgumentException("Invalid price range");
            }

            var services = await _unitOfWork.Services.GetServicesByPriceRangeAsync(minPrice, maxPrice, cancellationToken);
            return _mapper.Map<IEnumerable<ServiceDto>>(services);
        }

        public async Task<ServiceDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default)
        {
            var service = _mapper.Map<Service>(dto);

            await _unitOfWork.Services.AddAsync(service, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Service created with ID: {ServiceId}", service.ServiceId);

            return _mapper.Map<ServiceDto>(service);
        }

        public async Task<ServiceDto> UpdateAsync(UpdateServiceDto dto, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceId, cancellationToken);

            if (service == null)
            {
                throw new KeyNotFoundException($"Service with ID {dto.ServiceId} not found");
            }

            _mapper.Map(dto, service);
            _unitOfWork.Services.Update(service);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Service updated with ID: {ServiceId}", service.ServiceId);

            return _mapper.Map<ServiceDto>(service);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);

            if (service == null)
            {
                return false;
            }

            _unitOfWork.Services.Delete(service);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Service deleted with ID: {ServiceId}", id);

            return true;
        }
        public async Task<PagedResult<ServiceDto>> GetPagedAsync(ServiceQueryParameters parameters, CancellationToken cancellationToken = default)
        {
            return await GetPagedWithSpecificationAsync(parameters, cancellationToken);
        }


        public async Task<PagedResult<ServiceDto>> GetPagedWithSpecificationAsync(
            ServiceQueryParameters parameters,
            CancellationToken cancellationToken = default)
        {
            var countSpec = new ServicesCountSpec(
                parameters.SearchTerm,
                parameters.MinPrice,
                parameters.MaxPrice);

            var totalCount = await _unitOfWork.Services.CountAsync(countSpec, cancellationToken);

            var skip = (parameters.Page - 1) * parameters.PageSize;

            var paginatedSpec = new ServicesPaginatedSpec(
                skip,
                parameters.PageSize,
                parameters.SearchTerm,
                parameters.MinPrice,
                parameters.MaxPrice,
                parameters.SortBy,
                parameters.SortOrder?.ToLower() == "desc");

            var services = await _unitOfWork.Services.ListAsync(paginatedSpec, cancellationToken);
            var dtos = _mapper.Map<IEnumerable<ServiceDto>>(services);

            return new PagedResult<ServiceDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            };
        }

        public async Task<IEnumerable<ServiceDto>> GetByPriceRangeWithSpecificationAsync(
            decimal minPrice,
            decimal maxPrice,
            CancellationToken cancellationToken = default)
        {
            var spec = new ServicesByPriceRangeSpec(minPrice, maxPrice);
            var services = await _unitOfWork.Services.ListAsync(spec, cancellationToken);
            return _mapper.Map<IEnumerable<ServiceDto>>(services);
        }
    }
}
