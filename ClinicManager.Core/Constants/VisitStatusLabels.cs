using ClinicManager.Core.Enums;

namespace ClinicManager.Core.Constants;

/// <summary>
/// Polskie etykiety statusow wizyty oraz dozwolone przejscia stanow.
/// Stan finalny ma pusty zbior nastepnikow - dla <c>Completed</c> i <c>Cancelled</c> wizyta jest zamknieta.
/// </summary>
public static class VisitStatusLabels
{
    public static IReadOnlyDictionary<VisitStatus, string> Polish { get; } =
        new Dictionary<VisitStatus, string>
        {
            [VisitStatus.Planned] = "Zaplanowana",
            [VisitStatus.InProgress] = "W trakcie",
            [VisitStatus.Completed] = "Zakonczona",
            [VisitStatus.Cancelled] = "Anulowana"
        };

    public static IReadOnlyDictionary<VisitStatus, IReadOnlyList<VisitStatus>> AllowedTransitions { get; } =
        new Dictionary<VisitStatus, IReadOnlyList<VisitStatus>>
        {
            [VisitStatus.Planned] = new[] { VisitStatus.InProgress, VisitStatus.Cancelled },
            [VisitStatus.InProgress] = new[] { VisitStatus.Completed, VisitStatus.Cancelled },
            [VisitStatus.Completed] = Array.Empty<VisitStatus>(),
            [VisitStatus.Cancelled] = Array.Empty<VisitStatus>()
        };

    public static string GetLabel(VisitStatus status) =>
        Polish.TryGetValue(status, out var label) ? label : status.ToString();

    public static IReadOnlyList<VisitStatus> GetNextStatuses(VisitStatus current) =>
        AllowedTransitions.TryGetValue(current, out var next) ? next : Array.Empty<VisitStatus>();

    public static bool CanTransition(VisitStatus from, VisitStatus to) =>
        AllowedTransitions.TryGetValue(from, out var next) && next.Contains(to);
}
