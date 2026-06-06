namespace ClimbConnect.API.Dtos;

public record ProgressUpdateDto(
    string Status,
    int Attempts,
    string? Notes,
    DateOnly Date
);
