using System.ComponentModel.DataAnnotations;
using ClimbConnect.API.Services;

namespace ClimbConnect.API.Dtos;

/// <summary>
/// Prüft, ob ein Grad in der französischen Grad-Tabelle (<see cref="GradeConversionService"/>) vorkommt.
/// Leere Werte sind erlaubt – ob ein Grad Pflicht ist, regelt [Required].
/// </summary>
public class FrenchGradeAttribute : ValidationAttribute
{
    /// <summary>Setzt die Fehlermeldung, die der ValidationFilter zurückgibt.</summary>
    public FrenchGradeAttribute()
        : base("{0} ist kein gültiger Grad der französischen Skala (z. B. 6a, 6a+, 7b).") { }

    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value is null || value is string s && (string.IsNullOrWhiteSpace(s) || GradeConversionService.IsValid(s));
}
