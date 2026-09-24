namespace ClimbConnect.API.Models;

/// <summary>Erlaubte Werte für Status und Begehungsart bei Fortschrittseinträgen.</summary>
public static class ProgressConst
{
    public static readonly string[] Statuses = ["Projekt", "Rotpunkt", "Flash", "Onsight"];
    public static readonly string[] Styles   = ["Toprope", "Vorstieg"];
}
