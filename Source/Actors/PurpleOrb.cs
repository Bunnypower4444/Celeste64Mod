
namespace Celeste64;

public class PurpleOrb : Actor, IHaveSprites, IPickup, IHaveModels, ICastPointShadow
{
    public float PickupRadius => 16;
    public float PointShadowAlpha { get ; set ; } = 1;
    
    public SkinnedModel Model;
	public ParticleSystem Particles;
    public bool IsCutscene { get; private set; }
	public float CutsceneHeight { get; private set; } = 0;
	
	private float tCooldown = 0;

    public PurpleOrb(float? cutsceneHeight = null)
    {
        IsCutscene = cutsceneHeight != null;
		CutsceneHeight = cutsceneHeight ?? 0;
        LocalBounds = new BoundingBox(Vec3.Zero, 4);
		Model = new(Assets.Models["refill_gem_double"]);
		Model.Flags = ModelFlags.Default;
		Particles = new(32, new ParticleTheme()
		{
			Rate = 10.0f,
			Sprite = "particle-star",
			Life = 0.5f,
			Gravity = new Vec3(0, 0, 90),
			Size = 2.5f
		});
    }

	public override void Added()
	{
		LocalBounds = new BoundingBox(Vec3.Zero, 3);
	}

	public override void Update()
	{
		Particles.SpawnParticle(
			Position + new Vec3(6 - World.Rng.Float() * 12, 6 - World.Rng.Float() * 12, 6 - World.Rng.Float() * 12),
			new Vec3(0, 0, 0), 1, World.DeltaTime);
		Particles.Update(World.DeltaTime);
		if (tCooldown > 0)
			tCooldown -= World.DeltaTime;
	}

    public void CollectModels(List<(Actor Actor, Model Model)> populate)
	{
		if (tCooldown <= 0)
		{
			Model.Transform =
				Matrix.CreateScale(2.0f) *
				Matrix.CreateTranslation(Vec3.UnitZ * MathF.Sin(World.GeneralTimer * 2.0f) * 2) *
				Matrix.CreateRotationZ(World.GeneralTimer * 3.0f);
			populate.Add((this, Model));
		}
	}

    public void CollectSprites(List<Sprite> populate)
	{
		Particles.CollectSprites(Position, World, populate);
	}

    public void Pickup(Player player)
    {
		if (tCooldown > 0) return;

        player.PurpleOrbLaunch(this);
		tCooldown = 5;

		// Badeline stuff
		var badeline = new Badeline();
		World.Add(badeline);
		badeline.PurpleOrbLaunch(this, player.Facing, true);
    }
}