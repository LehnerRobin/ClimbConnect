using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

/// <summary>Daten für einen neuen Kommentar. PhotoUrl ist optional.</summary>
public record CommentCreateDto(
    [Required] [StringLength(2000, MinimumLength = 1)] string  Text,
    [MaxLength(500)]                                   string? PhotoUrl = null
);
