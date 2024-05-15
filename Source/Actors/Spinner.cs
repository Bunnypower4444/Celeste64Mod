

namespace Celeste64;

public class Spinner : Attacher, IHaveModels, IPickup
{
    private static Color color = Color.White;
    public static Color ColorTheme { get => color; set {
        color = value;
        foreach (var mat in Assets.Models["spike_ball"].Materials)
        {
            mat.Color = value;
        }
    } }

    public float PickupRadius => 8;
    /* public override Vec3 AttachNormal =>
        World.SolidWallCheckNearest(Position, PickupRadius * 2, out var hit) ?
        (hit.Point - Position).Normalized() :
        default; */

    private readonly SkinnedModel model;
    private readonly bool noAttach;

    public Spinner(bool noAttach = false)
    {
        model = new(Assets.Models["spike_ball"])
        {
            Flags = ModelFlags.Terrain,
            Transform = Matrix.CreateScale(2.0f)
        };
        this.noAttach = noAttach;
    }

    public override void Added()
    {
        base.Added();

        if (noAttach && AttachedTo is Solid solid)
        {
            solid.Attachers.Remove(this);
            AttachedTo = null;
        }
    }

    public void Pickup(Player player)
    {
        player.Kill();
    }

    public void CollectModels(List<(Actor, Model)> populate)
    {
        populate.Add((this, model));
    }
}