using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record AppointmentCreateDto(
    [Required] [StringLength(200, MinimumLength = 1)] string   Title,
    [Required]                                        DateTime Date,
    [MaxLength(500)]                                  string?  MeetingPoint,
    [MaxLength(2000)]                                 string?  Description,
    [Range(1, 9999)]                                  int?     MinParticipants,
    [Range(1, 9999)]                                  int?     MaxParticipants
);
