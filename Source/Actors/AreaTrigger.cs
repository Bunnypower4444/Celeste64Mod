
namespace Celeste64;

/// <summary>
/// An area of the map that can trigger events when the player enters/exits the area
/// </summary>
public class AreaTrigger(string id) : Actor
{
    private readonly List<Action<AreaTrigger>> enterActions = [];
    private readonly List<Action<AreaTrigger>> inTriggerActions = [];
    private readonly List<Action<AreaTrigger>> exitActions = [];
    private readonly List<Action<AreaTrigger>> removedEnterActions = [];
    private readonly List<Action<AreaTrigger>> removedInTriggerActions = [];
    private readonly List<Action<AreaTrigger>> removedExitActions = [];
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

    public void AddInTriggerAction(Action<AreaTrigger> action)
    {
        inTriggerActions.Add(action);
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
        if (enterActions.Contains(action))
        {
            removedEnterActions.Add(action);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes the specified in-trigger action.
    /// </summary>
    /// <param name="action">The action to be removed.</param>
    /// <returns>True if the action was successfully removed; otherwise, false.</returns>
    public bool RemoveInTriggerAction(Action<AreaTrigger> action)
    {
        if (inTriggerActions.Contains(action))
        {
            removedInTriggerActions.Add(action);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes the specified exit action.
    /// </summary>
    /// <param name="action">The action to be removed.</param>
    /// <returns>True if the action was successfully removed; otherwise, false.</returns>
    public bool RemoveExitAction(Action<AreaTrigger> action)
    {
        if (exitActions.Contains(action))
        {
            removedExitActions.Add(action);
            return true;
        }
        return false;
    }

    public void RunEnterActions()
    {
        foreach (var action in enterActions)
        {
            action(this);
        }
    }

    public void RunInTriggerActions()
    {
        foreach (var action in inTriggerActions)
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
        if (inTrigger)
        {
            if (!PlayerWasInTrigger) RunEnterActions();
            RunInTriggerActions();
        }
        else if (!inTrigger && PlayerWasInTrigger) RunExitActions();

        PlayerWasInTrigger = inTrigger;

        // Removing actions this way so an action can remove itself when it is running
        foreach (var action in removedEnterActions)
            enterActions.Remove(action);
        removedEnterActions.Clear();
        foreach (var action in removedInTriggerActions)
            inTriggerActions.Remove(action);
        removedInTriggerActions.Clear();
        foreach (var action in removedExitActions)
            exitActions.Remove(action);
        removedExitActions.Clear();
    }
}