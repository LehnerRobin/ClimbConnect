using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record ProgressCreateDto(
    [Required] [Range(1, int.MaxValue)] int     RouteId,
    [Required] [MaxLength(20)]          string  Status,
    [Required] [MaxLength(20)]          string  ClimbingStyle,
    [Range(1, 9999)]                    int     Attempts,
    [MaxLength(1000)]                   string? Notes,
    [Required]                          DateOnly Date,
    [MaxLength(20)]                     string?  SubjectiveGrade,
    [MaxLength(500)]                    string?  SubjectiveGradeComment
);
