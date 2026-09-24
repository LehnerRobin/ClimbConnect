using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record UserProfileUpdateDto(
    [MaxLength(500)] string? Bio,
    [MaxLength(20)]  string? PreferredGradeScale
);
