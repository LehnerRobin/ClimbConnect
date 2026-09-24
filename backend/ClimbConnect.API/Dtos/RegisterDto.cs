using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record RegisterDto(
    [Required] [StringLength(50, MinimumLength = 3)] string Username,
    [Required] [EmailAddress] [StringLength(200)]    string Email,
    [Required] [MinLength(8)] [MaxLength(100)]        string Password
);
