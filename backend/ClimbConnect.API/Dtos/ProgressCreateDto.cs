namespace ClimbConnect.API.Dtos;

public record ProgressCreateDto(
    int UserId,
    int RouteId,
    string Status,
    int Attempts,
    string? Notes,
    DateOnly Date
);
