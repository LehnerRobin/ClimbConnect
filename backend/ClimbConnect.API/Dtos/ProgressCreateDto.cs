namespace ClimbConnect.API.Dtos;

public record ProgressCreateDto(
    int UserId,
    int RouteId,
    string Status,
    string ClimbingStyle,
    int Attempts,
    string? Notes,
    DateOnly Date,
    string? SubjectiveGrade,
    string? SubjectiveGradeComment
);
