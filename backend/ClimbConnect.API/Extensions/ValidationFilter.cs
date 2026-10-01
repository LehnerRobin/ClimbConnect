using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ClimbConnect.API.Dtos;

namespace ClimbConnect.API.Extensions;

/// <summary>
/// Prüft die DataAnnotations ([Required], [MinLength], [Range] …) aller DTO-Parameter
/// eines Endpoints. Minimal APIs werten diese Attribute in .NET 8 nicht von selbst aus –
/// ohne diesen Filter wären sie wirkungslos.
/// </summary>
public class ValidationFilter : IEndpointFilter
{
    private static readonly string DtoNamespace = typeof(LoginDto).Namespace!;

    /// <summary>Bricht mit 400 ab, sobald ein DTO-Parameter ungültig ist; sonst läuft der Endpoint normal weiter.</summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null || argument.GetType().Namespace != DtoNamespace) continue;

            var errors = Validate(argument);
            if (errors.Count > 0)
                return Results.BadRequest(new { error = string.Join(" ", errors) });
        }

        return await next(context);
    }

    /// <summary>
    /// Die DTOs sind Records: Die Attribute hängen an den Konstruktor-Parametern,
    /// der Wert wird über die gleichnamige Property gelesen.
    /// </summary>
    private static List<string> Validate(object dto)
    {
        var type   = dto.GetType();
        var errors = new List<string>();

        foreach (var parameter in type.GetConstructors().First().GetParameters())
        {
            var name  = parameter.Name!;
            var value = type.GetProperty(name)?.GetValue(dto);

            foreach (var attribute in parameter.GetCustomAttributes<ValidationAttribute>())
            {
                if (!attribute.IsValid(value))
                    errors.Add(Message(attribute, name));
            }
        }

        return errors;
    }

    /// <summary>Deutsche Fehlermeldung passend zum verletzten Attribut.</summary>
    private static string Message(ValidationAttribute attribute, string name) => attribute switch
    {
        RequiredAttribute        => $"{name} ist erforderlich.",
        EmailAddressAttribute    => $"{name} ist keine gültige E-Mail-Adresse.",
        MinLengthAttribute min   => $"{name} muss mindestens {min.Length} Zeichen lang sein.",
        MaxLengthAttribute max   => $"{name} darf höchstens {max.Length} Zeichen lang sein.",
        StringLengthAttribute sl => $"{name} muss zwischen {sl.MinimumLength} und {sl.MaximumLength} Zeichen lang sein.",
        RangeAttribute range     => $"{name} muss zwischen {range.Minimum} und {range.Maximum} liegen.",
        _                        => attribute.FormatErrorMessage(name)
    };
}
