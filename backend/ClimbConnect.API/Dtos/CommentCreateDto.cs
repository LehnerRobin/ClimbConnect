namespace ClimbConnect.API.Dtos;

/// <summary>Daten für einen neuen Kommentar. PhotoUrl ist optional.</summary>
public record CommentCreateDto(string Text, string? PhotoUrl = null);
