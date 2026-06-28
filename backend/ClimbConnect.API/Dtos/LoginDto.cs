using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record LoginDto(
    [Required] [EmailAddress] [MaxLength(200)] string Email,
    [Required] [MaxLength(100)]                string Password
);
