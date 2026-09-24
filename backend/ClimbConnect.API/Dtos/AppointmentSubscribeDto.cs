using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record AppointmentSubscribeDto([MaxLength(500)] string? Comment);
