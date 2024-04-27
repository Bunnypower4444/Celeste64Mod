
namespace Celeste64;

public class Spring : Attacher, IHaveModels, IPickup
{
	public SkinnedModel Model;

	public float PickupRadius => 16;

	public override Vec3 AttachNormal => Direction switch {
		SpringDirection.Up => -Vec3.UnitZ,
		SpringDirection.Side => -new Vec3(Facing, 0),
		_ => -Vec3.UnitZ
	};

	private float tCooldown = 0;

	public enum SpringDirection { Up, Side };

	public SpringDirection Direction;

	public Spring(string direction)
	{
		if (Enum.TryParse<SpringDirection>(direction, true, out var dir))
			Direction = dir;
		else
			Direction = SpringDirection.Up;

		Model = new SkinnedModel(Assets.Models["spring_board"]);
		Model.Transform = Matrix.CreateScale(8.0f);
		Model.SetLooping("Spring", false);
		Model.Play("Idle");
		if (Direction == SpringDirection.Side)
			Model.Transform *= Matrix.CreateRotationX(MathF.PI / 2);

		LocalBounds = new(Position + Vec3.UnitZ * 4, 16);
	}

	public override void Update()
	{
		Model.Update(World.DeltaTime);

		if (tCooldown > 0)
		{
			tCooldown -= World.DeltaTime;
			if (tCooldown <= 0)
				UpdateOffScreen = false;
		}
	}

	public void CollectModels(List<(Actor Actor, Model Model)> populate)
	{
		populate.Add((this, Model));
	}

	public void Pickup(Player player)
	{
		if (tCooldown <= 0)
		{
			UpdateOffScreen = true;
			Audio.Play(Sfx.sfx_springboard, Position);
			tCooldown = 1.0f;
			Model.Play("Spring", true);
			player.Spring(this);

			if (AttachedTo is FallingBlock fallingBlock)
				fallingBlock.Trigger();
		}
	}
}
