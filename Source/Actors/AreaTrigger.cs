
using TriggerAction = System.Action<Celeste64.AreaTrigger, Celeste64.Player>;

namespace Celeste64;

/// <summary>
/// An area of the map that can trigger events when the player enters/exits the area
/// </summary>
public class AreaTrigger(string id) : Actor
{
    private readonly List<TriggerAction> enterActions = [];
    private readonly List<TriggerAction> inTriggerActions = [];
    private readonly List<TriggerAction> exitActions = [];
    private readonly List<TriggerAction> removedEnterActions = [];
    private readonly List<TriggerAction> removedInTriggerActions = [];
    private readonly List<TriggerAction> removedExitActions = [];
    public readonly string ID = id;
    public bool PlayerIsInTrigger => World.Get<Player>() is {} player && WorldBounds.Contains(player.Position);
    public bool PlayerWasInTrigger { get; private set; } = false;

    public static AreaTrigger? GetAreaTrigger(World world, string id)
    {
        return world.All<AreaTrigger>().Find(actor => actor is AreaTrigger areaTrigger && areaTrigger.ID == id) as AreaTrigger;
    }

    public void AddEnterAction(TriggerAction action)
    {
        enterActions.Add(action);
    }

    public void AddInTriggerAction(TriggerAction action)
    {
        inTriggerActions.Add(action);
    }

    public void AddExitAction(TriggerAction action)
    {
        exitActions.Add(action);
    }

    /// <summary>
    /// Removes the specified enter action.
    /// </summary>
    /// <param name="action">The action to be removed.</param>
    /// <returns>True if the action was successfully removed; otherwise, false.</returns>
    public bool RemoveEnterAction(TriggerAction action)
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
    public bool RemoveInTriggerAction(TriggerAction action)
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
    public bool RemoveExitAction(TriggerAction action)
    {
        if (exitActions.Contains(action))
        {
            removedExitActions.Add(action);
            return true;
        }
        return false;
    }

    public void RunEnterActions(Player player)
    {
        foreach (var action in enterActions)
        {
            action(this, player);
        }
    }

    public void RunInTriggerActions(Player player)
    {
        foreach (var action in inTriggerActions)
        {
            action(this, player);
        }
    }

    public void RunExitActions(Player player)
    {
        foreach (var action in exitActions)
        {
            action(this, player);
        }
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        var inTrigger = PlayerIsInTrigger;
        var player = World.Get<Player>();
        if (inTrigger)
        {
            if (!PlayerWasInTrigger) RunEnterActions(player!);
            RunInTriggerActions(player!);
        }
        else if (!inTrigger && PlayerWasInTrigger) RunExitActions(player!);

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