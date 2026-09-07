using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Timeline.Models;
using Dharmatlas.Timeline.Navigation;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class EventFocusNavigatorTests
{
    private static IReadOnlyList<EventSummary> Sample() =>
        new List<EventSummary>
        {
            new(MakeId(1), "Council", "650 CE", Certainty.Documented, "council", "China", Array.Empty<EntityId>(), "/events/1"),
            new(EntityId.From(Guid.Parse("00000000-0000-0000-0000-000000000002")), "Translation", "c. 150 CE", Certainty.Probable, "translation", "India", Array.Empty<EntityId>(), "/events/2")
        };

    private static EntityId MakeId(int n) => EntityId.From(Guid.Parse($"00000000-0000-0000-0000-00000000000{n}"));

    [Fact]
    public void Next_and_previous_move_within_bounds()
    {
        var nav = new EventFocusNavigator(Sample());

        Assert.Equal(1, nav.Next(0));
        Assert.Equal(1, nav.Next(1)); // clamps at last
        Assert.Equal(0, nav.Previous(1));
        Assert.Equal(0, nav.Previous(0)); // clamps at first
    }

    [Fact]
    public void Accessible_label_is_non_visual_equivalent()
    {
        var nav = new EventFocusNavigator(Sample());

        Assert.Equal("Event 1 of 2: Council, 650 CE, Documented.", nav.AccessibleLabel(0));
        Assert.Equal("Event 2 of 2: Translation, c. 150 CE, Probable.", nav.AccessibleLabel(1));
    }

    [Fact]
    public void Keyboard_focus_exposes_detail_navigation()
    {
        var nav = new EventFocusNavigator(Sample());

        Assert.Equal("/events/2", nav.DetailRoute(1));
        Assert.Equal("/events/1", nav.DetailRoute(nav.First()));
    }

    [Fact]
    public void Empty_list_yields_no_focusable_event()
    {
        var nav = new EventFocusNavigator(Array.Empty<EventSummary>());

        Assert.Equal(0, nav.Count);
        Assert.Null(nav.Focus(0));
        Assert.Equal(string.Empty, nav.AccessibleLabel(0));
    }
}
