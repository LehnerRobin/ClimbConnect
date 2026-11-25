using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

public record AppointmentCreateDto(
    [Required] [StringLength(200, MinimumLength = 1)] string   Title,
    [Required]                                        DateTime Date,
    [MaxLength(500)]                                  string?  MeetingPoint,
    [MaxLength(2000)]                                 string?  Description,
    // Allein klettern braucht keinen Termin → mindestens 2; leer = Standard 2
    [Range(2, 9999, ErrorMessage = "Mindestens 2 Teilnehmer")] int? MinParticipants,
    // Leer = "Open End" (kein Limit)
    [Range(2, 9999, ErrorMessage = "Maximum muss mindestens 2 sein")] int? MaxParticipants
);
