using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

/// <summary>
/// Neues Feedback an das Team (#244). Foto und Bezug zu Gebiet oder Route sind optional –
/// das Frontend setzt den Bezug, wenn der User gerade auf dieser Seite ist.
/// </summary>
public record FeedbackCreateDto(
    [Required] [StringLength(2000, MinimumLength = 1)] string  Text,
    [MaxLength(500)]                                   string? PhotoUrl = null,
    int? AreaId  = null,
    int? RouteId = null
);

/// <summary>Gelesen-Status ändern (Admin).</summary>
public record FeedbackReadDto(bool IsRead);

/// <summary>Feedback in der Admin-Liste – mit Namen von Verfasser, Gebiet und Route.</summary>
public record FeedbackDto(
    int      Id,
    int      UserId,
    string   Username,
    string   Text,
    string?  PhotoUrl,
    int?     AreaId,
    string?  AreaName,
    int?     RouteId,
    string?  RouteName,
    bool     IsRead,
    DateTime CreatedAtUtc
);
