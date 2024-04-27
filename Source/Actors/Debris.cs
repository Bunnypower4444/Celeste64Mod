
namespace Celeste64;

public class Debris : Actor, IRecycle, IHaveSprites
{
	public Vec3 Velocity;
	public Subtexture Image;
	public float Timer;

	public void Init(Vec3 position, Vec3 velocity, string image)
	{
		Position = position;
		Velocity = velocity;
		Image = Assets.Subtextures[image];
		Timer = 0.0f;
		UpdateOffScreen = true;
	}

	public override void Update()
	{
		Position += Velocity * World.DeltaTime;
		Velocity.Z -= 200 * World.DeltaTime;

		var vxy = Velocity.XY();
		vxy = Calc.Approach(vxy, Vec2.Zero, 400 * World.DeltaTime);
		Velocity = Velocity.WithXY(vxy);

		Timer += World.DeltaTime;
		if (Timer > 2.0f)
			World.Destroy(this);
	}

	public void CollectSprites(List<Sprite> populate)
	{
		populate.Add(Sprite.CreateBillboard(World, Position, Image, 2, Color.White));
	}
}
