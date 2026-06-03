namespace ClimbConnect.API.Dtos;

public record RouteCreateDto(
    string Name,
    string? Grade,
    string? Sector
);
