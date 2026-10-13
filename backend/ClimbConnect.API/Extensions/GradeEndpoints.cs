using ClimbConnect.API.Services;

namespace ClimbConnect.API.Extensions;

/// <summary>Endpoint für die Grad-Tabelle.</summary>
public static class GradeEndpoints
{
    /// <summary>
    /// GET /api/grades – alle gültigen Grade (französisch, UIAA, amerikanisch), sortiert von leicht
    /// nach schwer. Das Frontend baut daraus seine Auswahllisten, damit es keine eigene Liste braucht.
    /// </summary>
    public static void MapGradeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/grades", () => Results.Ok(GradeConversionService.GetGrades()))
            .WithName("GetGrades")
            .WithTags("Grades");
    }
}
