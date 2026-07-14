namespace IRM.Domain;

/// <summary>Kleine Sammlung wiederkehrender Invarianten-Prüfungen, hält die Entities schlank.</summary>
internal static class Guard
{
    /// <summary>Stellt sicher, dass ein Text nicht leer ist, und gibt ihn getrimmt zurück.</summary>
    public static string NotNullOrWhiteSpace(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException($"'{field}' darf nicht leer sein.");
        return value.Trim();
    }
}
