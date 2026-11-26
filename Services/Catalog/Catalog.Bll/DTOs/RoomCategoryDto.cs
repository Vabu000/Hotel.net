using System.ComponentModel.DataAnnotations;

namespace HotelCatalog.Bll.DTOs
{
    public class RoomCategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MaxOccupancy { get; set; }
        public decimal? AreaSquareMeters { get; set; }
        public List<ServiceDto> Services { get; set; } = new();
        public List<RoomServiceDto> ServiceAssignments { get; set; } = new();
        public int ServiceCount { get; set; }
    }

    public class RoomServiceDto
    {
        public int CategoryId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal AdditionalPrice { get; set; }
    }

    public class CreateRoomCategoryDto
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }

    public class UpdateRoomCategoryDto
    {
        [Required]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }

    public class AssignServiceToCategoryDto
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Range(typeof(decimal), "0", "1000000")]
        public decimal AdditionalPrice { get; set; }
    }
}
