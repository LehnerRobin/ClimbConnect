using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

/// <summary>Neue Rolle eines Users (#237): "admin" oder "user".</summary>
public record AdminRoleUpdateDto([Required] [MaxLength(20)] string Role);
