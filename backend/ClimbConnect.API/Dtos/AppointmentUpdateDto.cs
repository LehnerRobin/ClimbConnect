namespace ClimbConnect.API.Dtos;

public record AppointmentUpdateDto(
    string Title,
    DateTime Date,
    string? MeetingPoint,
    string? Description,
    int? MinParticipants,
    int? MaxParticipants
);
