using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record ReportStatusUpdateDto([Required] [MaxLength(20)] string Status);
