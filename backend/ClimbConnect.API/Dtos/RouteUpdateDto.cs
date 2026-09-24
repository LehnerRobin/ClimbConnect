using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record RouteUpdateDto(
    [Required] [StringLength(200, MinimumLength = 1)] string  Name,
    [MaxLength(20)]                                   string? Grade,
    [Range(1, 3000)]                                  int?    LengthMeters,
    [MaxLength(100)]                                  string? Style,
    [MaxLength(2000)]                                 string? Description
);
