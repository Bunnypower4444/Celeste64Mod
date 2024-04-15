
namespace Celeste64;

public sealed class Coin : Actor, IHaveModels, IHaveSprites, IPickup, ICastPointShadow
{
	public SkinnedModel Model;
	public float PickupRadius => 20;
	public bool Collected { get; private set; }
	public float PointShadowAlpha { get; set; }

	private Color inactiveColor = 0x5fcde4;
	private Color collectedColor = 0xf141df;

	public Coin()
	{
		Model = new SkinnedModel(Assets.Models["coin"]);
		Model.Flags = ModelFlags.Default;
		Model.MakeMaterialsUnique();
		foreach (var mat in Model.Materials)
			mat.Color = inactiveColor;
		PointShadowAlpha = 1.0f;
		LocalBounds = new BoundingBox(Vec3.Zero, 16);
	}

	public void CollectSprites(List<Sprite> populate)
	{
		if (!Collected)
		{
			//Particles.CollectSprites(Position, World, populate);
			var haloPos = Position + Vec3.UnitZ * 2 + Vec3.Transform(Vec3.Zero, Model.Transform);
			populate.Add(Sprite.CreateBillboard(World, haloPos, "gradient", 10, inactiveColor * 0.50f));
		}
	}

    public void CollectModels(List<(Actor Actor, Model Model)> populate)
    {
		if (Collected)
		{
			Model.Flags = ModelFlags.Transparent;
			foreach (var mat in Model.Materials)
				mat.Color = collectedColor * 0.50f;
		}

		Model.Transform =
			Matrix.CreateScale(6.0f) *
			Matrix.CreateTranslation(Vec3.UnitZ * MathF.Sin(World.GeneralTimer * 2.0f) * 2) *
			Matrix.CreateRotationZ(World.GeneralTimer * 3.0f);

		populate.Add((this, Model));
    }

	public void Pickup(Player player)
	{
		if (!Collected)
		{
			Collected = true;
			if ((!AnyRemaining(World) && GroupName == string.Empty) || (!AnyRemaining(World, GroupName) && GroupName != string.Empty))
				Audio.Play(Sfx.sfx_touch_switch_last, Position);
			else
				Audio.Play(Sfx.sfx_touch_switch_any, Position);
		}
	}

	public static bool AnyRemaining(World world)
	{
		foreach (var it in world.All<Coin>())
		{
			if (!(it as Coin)!.Collected)
				return true;
		}

		return false;
	}

	public static bool AnyRemaining(List<Coin> coins)
	{
		foreach (var it in coins)
		{
			if (!it.Collected)
				return true;
		}

		return false;
	}

	public static bool AnyRemaining(World world, string groupName)
	{
		List<Actor> list = world.All<Coin>();
		foreach (var it in list)
		{
			if (it.GroupName == groupName && !(it as Coin)!.Collected)
				return true;
		}

		return false;
	}
}
