
namespace Celeste64;

public class DashPuzzleBlock(int dashes) : Solid, IUnlockStrawberry, IHaveSprites
{
    public bool Satisfied { get; private set; }

	private readonly int dashes = dashes;
	private bool ready = false;
	private float pulse = 0;
	private Color pulseColor;

    public void CollectSprites(List<Sprite> populate)
    {
		if (ready && !Satisfied && pulse > 0)
		{
			var haloPos = Position + Vec3.UnitZ * 2 + Vec3.Transform(Vec3.Zero, Model.Transform);
			populate.Add(Sprite.CreateBillboard(World, haloPos, "gradient", 50 * pulse, pulseColor * 0.50f) with { Post = true });
			populate.Add(Sprite.CreateBillboard(World, Position, "ring", pulse * pulse * 40, pulseColor * .4f) with { Post = true });
			populate.Add(Sprite.CreateBillboard(World, Position, "ring", pulse * 50, pulseColor * .4f) with { Post = true });
		}
    }

    public override void Update()
    {
		base.Update();

		if (!Satisfied && !ready && World.Get<Player>() is {} player && player.Dashes == dashes && player.MaxDashes <= dashes && HasPlayerRider())
		{
			pulseColor = player.Hair.Color;
			ready = true;
			pulse = 1;
			TShake = 1.0f;
			Audio.Play(Sfx.sfx_secret, Position);
		}

		if (ready && pulse > 0)
		{
			pulse -= World.DeltaTime;
			if (pulse <= 0)
				Satisfied = true;
		}
    }
}
