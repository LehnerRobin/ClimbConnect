using System.ComponentModel.DataAnnotations;

namespace ClimbConnect.API.Dtos;

/// <summary>Bestätigung zum Löschen des eigenen Kontos (#232): das aktuelle Passwort.</summary>
public record DeleteAccountDto(
    [Required] string Password
);
