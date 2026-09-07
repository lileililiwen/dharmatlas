using Dharmatlas.Timeline.Models;

namespace Dharmatlas.Timeline.Navigation;

/// <summary>
/// Keyboard-accessible focus model for the timeline. Provides ordered focus
/// movement and a non-visual, screen-reader-friendly label for each event so
/// exploration works without pointer input or a rendered chart.
/// </summary>
public sealed class EventFocusNavigator
{
    private readonly IReadOnlyList<EventSummary> _items;

    public EventFocusNavigator(IReadOnlyList<EventSummary> items) => _items = items;

    public int Count => _items.Count;

    public EventSummary? Focus(int index) =>
        index >= 0 && index < _items.Count ? _items[index] : null;

    public int Next(int current) => Math.Min(current + 1, Math.Max(_items.Count - 1, 0));

    public int Previous(int current) => Math.Max(current - 1, 0);

    public int First() => 0;

    public int Last() => Math.Max(_items.Count - 1, 0);

    /// <summary>
    /// Non-visual equivalent of focusing an event: title, date, and certainty.
    /// </summary>
    public string AccessibleLabel(int index)
    {
        var e = Focus(index);
        return e is null
            ? string.Empty
            : $"Event {index + 1} of {_items.Count}: {e.Title}, {e.DisplayDate}, {e.Certainty}.";
    }

    /// <summary>Detail route for keyboard activation of the focused event.</summary>
    public string? DetailRoute(int index) => Focus(index)?.DetailRoute;
}
