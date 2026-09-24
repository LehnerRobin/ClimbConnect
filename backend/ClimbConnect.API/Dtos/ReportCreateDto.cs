using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record ReportCreateDto(
    int?                                               AreaId,
    int?                                               RouteId,
    [Required] [StringLength(2000, MinimumLength = 1)] string  Text,
    [MaxLength(500)]                                   string? PhotoUrl,
    [Required] [MaxLength(10)]                         string  Severity
);
