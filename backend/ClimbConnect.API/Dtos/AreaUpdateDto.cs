using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record AreaUpdateDto(
    [Required] [StringLength(200, MinimumLength = 1)] string  Name,
    [MaxLength(200)]                                  string? Location,
    [MaxLength(2000)]                                 string? Description,
    [MaxLength(500)]                                  string? ImageUrl
);
