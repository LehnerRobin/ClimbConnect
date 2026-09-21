namespace ClimbConnect.API.Dtos;

public record ReportCreateDto(
    int? AreaId,
    int? RouteId,
    string Text,
    string? PhotoUrl,
    string Severity
);
