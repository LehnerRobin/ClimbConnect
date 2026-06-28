using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record SectorCreateDto(
    [Required] [StringLength(200, MinimumLength = 1)] string  Name,
    [MaxLength(2000)]                                 string? Description
);
