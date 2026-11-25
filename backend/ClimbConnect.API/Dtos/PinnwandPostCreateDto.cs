using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

/// <summary>Neuer Pinnwand-Beitrag: nur Text (keine Fotos), 1–1000 Zeichen.</summary>
public record PinnwandPostCreateDto(
    [Required] [StringLength(1000, MinimumLength = 1)] string Text
);

/// <summary>Pinnwand-Beitrag, wie er an das Frontend geht (Vertrag in PINNWAND-MELDEN-API.md).</summary>
public record PinnwandPostDto(
    int      Id,
    int      AppointmentId,
    int      AuthorId,
    string   AuthorUsername,
    string   Text,
    DateTime CreatedAtUtc,
    bool     ReportedByMe
);
