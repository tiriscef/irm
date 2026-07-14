namespace IRM.Domain;

/// <summary>
/// Ein Zubereitungsschritt eines Rezepts (Value Object). <see cref="Order"/> ergibt sich
/// aus der Position im Rezept und wird vom <see cref="Recipe"/> vergeben.
/// </summary>
public sealed record Step
{
    public int Order { get; }
    public string Instruction { get; }

    public Step(int order, string instruction)
    {
        if (order < 1)
            throw new DomainValidationException("Die Reihenfolge eines Schritts beginnt bei 1.");
        Order = order;
        Instruction = Guard.NotNullOrWhiteSpace(instruction, nameof(instruction));
    }
}
