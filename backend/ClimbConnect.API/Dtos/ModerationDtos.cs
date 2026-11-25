using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

/// <summary>
/// Meldung eines Pinnwand-Beitrags (#264). Ob der Grund gültig ist und ob bei „Sonstiges" ein
/// Kommentar dabei ist, prüft der Endpoint (die Fehlertexte sind dort verständlicher).
/// </summary>
public record PostReportCreateDto(
    [Required] string Reason,
    [StringLength(500)] string? Comment
);

/// <summary>Eine Meldung im Fall – wer, warum, wann.</summary>
public record CaseReportDto(
    int      Id,
    int      ReporterId,
    string   ReporterUsername,
    string   Reason,
    string?  Comment,
    DateTime CreatedAtUtc
);

/// <summary>Kopie eines Beitrags im Fall. Leere <c>Reports</c> = Beitrag dient nur als Kontext.</summary>
public record CasePostDto(
    int      Id,
    int      OriginalPostId,
    int      AuthorId,
    string   AuthorUsername,
    string   Text,
    DateTime CreatedAtUtc,
    bool     DeletedByAuthor,
    List<CaseReportDto> Reports
);

/// <summary>Fall in der Admin-Liste (Vertrag in PINNWAND-MELDEN-API.md).</summary>
public record ModerationCaseSummaryDto(
    int      Id,
    int      AppointmentId,
    string   AppointmentTitle,
    DateTime AppointmentDate,
    string?  AreaName,
    bool     AppointmentDeleted,
    DateTime CreatedAtUtc,
    DateTime LastReportAtUtc,
    DateTime DeleteAfterUtc,
    int      ReportCount,
    int      ReportedPostCount,
    int      PostCount
);

/// <summary>Fall mit dem ganzen kopierten Verlauf, älteste Beiträge zuerst.</summary>
public record ModerationCaseDetailDto(
    int      Id,
    int      AppointmentId,
    string   AppointmentTitle,
    DateTime AppointmentDate,
    string?  AreaName,
    bool     AppointmentDeleted,
    DateTime CreatedAtUtc,
    DateTime LastReportAtUtc,
    DateTime DeleteAfterUtc,
    int      ReportCount,
    int      ReportedPostCount,
    int      PostCount,
    List<CasePostDto> Posts
);
