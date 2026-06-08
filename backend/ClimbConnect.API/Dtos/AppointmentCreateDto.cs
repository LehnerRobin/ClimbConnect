namespace ClimbConnect.API.Dtos;

public record AppointmentCreateDto(
    string Title,
    DateTime Date,
    string? MeetingPoint,
    string? Description,
    int? MinParticipants,
    int? MaxParticipants
);
