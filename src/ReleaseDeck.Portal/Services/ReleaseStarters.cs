using ReleaseDeck.Core;

namespace ReleaseDeck.Portal.Services;

/// <summary>Original planning examples without fabricated videos, customers, remote objects or team activity.</summary>
public static class ReleaseStarters
{
    public static readonly string[] Names = ["A small observatory", "Repair bench", "City sound walks"];
    /// <summary>Creates a separate editable three-episode plan, leaving required audience/file decisions for its creator.</summary>
    public static ReleaseProject Create(int index) => index switch
    {
        0 => new() { Title = Names[0], Description = "A three-part introduction to observing the night sky from an ordinary window.", Episodes = [Make("Find a dark corner", "Compare the light around three possible observing spots. Explain what changes when a nearby lamp is shielded and finish with a simple location checklist."), Make("Keep a sky journal", "Record the direction, time and weather before making a sketch. Compare two observations without claiming the sky stayed identical."), Make("Share the view", "Build a short guided observation for a friend: describe where to look, what to expect and a comfortable stopping point.")] },
        1 => new() { Title = Names[1], Description = "Small practical repairs with an emphasis on inspecting before replacing.", Episodes = [Make("Listen to the loose hinge", "Show a loose cabinet hinge, identify its mounting points and compare the movement before and after tightening the appropriate screws. Stop if the wood is damaged."), Make("Make a repair kit", "Lay out the hand tools used in this series. Explain the job of each item, safe storage and what a beginner does not need to buy."), Make("Test the repair", "Repeat the original movement test, record what changed and explain when a repair should be handed to a qualified professional.")] },
        _ => new() { Title = Names[2], Description = "Three short sound-led walks using recordings made with permission.", Episodes = [Make("The station before sunrise", "Introduce the empty platform through three short original recordings. Include a location description without recording identifiable conversations."), Make("A market changes tempo", "Follow one route at two different times. Describe the change in rhythm and identify which sounds were captured with explicit permission."), Make("A quiet route home", "End the series with a slower walk, a short listening exercise and credits for every contributed recording.")] }
    };
    static Episode Make(string title, string description) => new() { Title = title, Description = description, DefaultLanguage = "en", CategoryId = "27" };
}
