namespace ClimbConnect.API.Dtos;

public record ProgressUpdateDto(
    string Status,
    string ClimbingStyle,
    int Attempts,
    string? Notes,
    DateOnly Date,
    string? SubjectiveGrade,
    string? SubjectiveGradeComment
);
