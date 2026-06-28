using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

/// <summary>Payload zum Ändern des eigenen Passworts.</summary>
public record ChangePasswordDto(
    [Required] string CurrentPassword,
    [Required][MinLength(8)][MaxLength(100)] string NewPassword
);
