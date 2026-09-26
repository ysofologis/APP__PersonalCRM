namespace PersonalCrm.Core.Domain;

/// <summary>
/// Pure, side-effect-free calculation of how "warm" a relationship feels.
/// Driven by interaction recency and frequency, weighted by kind.
/// Result is in <c>[0, 1]</c>; <c>0</c> means "never talked", <c>1</c>
/// means "regular deep contact".
///
/// Not stored — recomputed on read or by a 5-minute background tick.
/// </summary>
public static class RelationshipStrength
{
    /// <summary>Decay half-life (in days). After 90 days an interaction has weight ~0.37.</summary>
    public const double DecayHalfLifeDays = 90.0;

    public static double Compute(
        Contact contact,
        IEnumerable<(InteractionKind Kind, DateTimeOffset OccurredAt)> recentInteractions,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentNullException.ThrowIfNull(recentInteractions);
        var interactions = recentInteractions as IList<(InteractionKind, DateTimeOffset)>
                           ?? recentInteractions.ToList();

        if (interactions.Count == 0)
        {
            return 0d;
        }

        double total = 0d;
        foreach (var (kind, occurredAt) in interactions)
        {
            var ageDays = Math.Max(0d, (now - occurredAt).TotalDays);
            var decay   = Math.Exp(-Math.Log(2d) * ageDays / DecayHalfLifeDays);
            total += Weight(kind) * decay;
        }

        // Logarithmic compression so a single daily ping saturates gently
        // rather than running away to infinity.
        var compressed = Math.Log(1d + total);
        return Math.Clamp(compressed / 4d, 0d, 1d);
    }

    public static double Weight(InteractionKind kind) => kind switch
    {
        InteractionKind.Meeting => 1.2,
        InteractionKind.Gift    => 1.5,
        InteractionKind.Event   => 0.8,
        InteractionKind.Call    => 1.0,
        InteractionKind.Message => 0.5,
        InteractionKind.Email   => 0.4,
        _                       => 0.5
    };
}
