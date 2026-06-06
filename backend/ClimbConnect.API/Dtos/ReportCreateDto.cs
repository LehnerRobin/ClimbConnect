namespace ClimbConnect.API.Dtos;

public record ReportCreateDto(
    int UserId,
    int? AreaId,
    int? RouteId,
    string Text,
    string? PhotoUrl
);
