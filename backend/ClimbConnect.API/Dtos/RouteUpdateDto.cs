namespace ClimbConnect.API.Dtos;

public record RouteUpdateDto(
    string Name,
    string? Grade,
    int? LengthMeters,
    string? Style,
    string? Description
);
