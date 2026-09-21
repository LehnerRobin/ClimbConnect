namespace ClimbConnect.API.Dtos;

public record RouteCreateDto(
    string Name,
    string? Grade,
    int? LengthMeters,
    string? Style,
    string? Description
);
