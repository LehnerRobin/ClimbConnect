namespace ClimbConnect.API.Dtos;

/// <summary>Autor eines Kommentars – bewusst nur Id und Username, keine E-Mail und kein Passwort-Hash.</summary>
public record CommentAuthorDto(int Id, string Username);

/// <summary>Kommentar, wie er an das Frontend ausgeliefert wird.</summary>
public record CommentDto(
    int              Id,
    int              UserId,
    int?             AreaId,
    int?             RouteId,
    string           Text,
    string?          PhotoUrl,
    DateTime         CreatedAtUtc,
    CommentAuthorDto User
);
