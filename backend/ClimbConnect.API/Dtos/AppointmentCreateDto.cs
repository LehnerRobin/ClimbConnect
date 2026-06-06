namespace ClimbConnect.API.Dtos;

public record AppointmentCreateDto(
    int CreatedByUserId,
    string Title,
    DateTime Date,
    string? MeetingPoint,
    string? Description,
    int? MinParticipants,
    int? MaxParticipants
);
