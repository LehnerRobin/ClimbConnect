namespace ClimbConnect.API.Extensions;

/// <summary>Namen der Rate-Limit-Regeln, damit Program.cs und Endpoints denselben Namen verwenden.</summary>
public static class RateLimitPolicies
{
    /// <summary>Begrenzung für Login und Registrierung (Anfragen pro Minute und IP).</summary>
    public const string Auth = "auth";
}
