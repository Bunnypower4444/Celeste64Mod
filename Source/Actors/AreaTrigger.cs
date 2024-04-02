
namespace Celeste64;

/// <summary>
/// An area of the map that can trigger events when the player enters/exits the area
/// </summary>
public class AreaTrigger(string id) : Actor
{
    private readonly List<Action<AreaTrigger>> enterActions = [];
    private readonly List<Action<AreaTrigger>> exitActions = [];
    public readonly string ID = id;
    public bool PlayerIsInTrigger => World.Get<Player>() is {} player && WorldBounds.Contains(player.Position);
    public bool PlayerWasInTrigger { get; private set; } = false;

    public static AreaTrigger? GetAreaTrigger(World world, string id)
    {
        return world.All<AreaTrigger>().Find(actor => actor is AreaTrigger areaTrigger && areaTrigger.ID == id) as AreaTrigger;
    }

    public void AddEnterAction(Action<AreaTrigger> action)
    {
        enterActions.Add(action);
    }

    public void AddExitAction(Action<AreaTrigger> action)
    {
        exitActions.Add(action);
    }

    /// <summary>
    /// Removes the specified enter action.
    /// </summary>
    /// <param name="action">The action to be removed.</param>
    /// <returns>True if the action was successfully removed; otherwise, false.</returns>
    public bool RemoveEnterAction(Action<AreaTrigger> action)
    {
        return enterActions.Remove(action);
    }

    /// <summary>
    /// Removes the specified exit action.
    /// </summary>
    /// <param name="action">The action to be removed.</param>
    /// <returns>True if the action was successfully removed; otherwise, false.</returns>
    public bool RemoveExitAction(Action<AreaTrigger> action)
    {
        return exitActions.Remove(action);
    }

    public void RunEnterActions()
    {
        foreach (var action in enterActions)
        {
            action(this);
        }
    }

    public void RunExitActions()
    {
        foreach (var action in exitActions)
        {
            action(this);
        }
    }

    public override void Update()
    {
        base.Update();
        var inTrigger = PlayerIsInTrigger;
        if (inTrigger && !PlayerWasInTrigger) RunEnterActions();
        else if (!inTrigger && PlayerWasInTrigger) RunExitActions();

        PlayerWasInTrigger = inTrigger;
    }
}