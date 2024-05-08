
namespace Celeste64;

public class PurpleOrb : Actor, IHaveSprites, IPickup, IHaveModels, ICastPointShadow
{
	public record struct PathNode
	{
		public Vec3 Position;
		public string? AutoContinueTriggerID;
		public float? CutsceneHeight;
		public readonly bool IsCutscene => CutsceneHeight != null;

		public PathNode(Vec3 position, string? autoContinueTriggerID = null, float? cutsceneHeight = null)
		{
			Position = position;
			AutoContinueTriggerID = autoContinueTriggerID;
			CutsceneHeight = cutsceneHeight;
		}
	}

    public float PickupRadius => 16;
    public float PointShadowAlpha { get ; set ; } = 1;
    
    public SkinnedModel Model;
	public ParticleSystem Particles;
	public TrailSystem TrailSystem = new() { FadeTime = 0.2f };
	
	private const float MoveTime = 0.4f;
	private bool canCollect = true;
	private int nodeIndex = 0;
	private bool isMoving = false;
	private int moveTargetIndex = 0;
	private float tMoveWait = 0;
	private float moveEase = 0;
	private int numTrails = 0;
	
	public readonly List<PathNode> PathNodes;
	public PathNode? CurrentNode => nodeIndex < PathNodes.Count ? PathNodes[nodeIndex] : null;

    public PurpleOrb(List<PathNode> nodes)
    {
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

		PathNodes = nodes;
    }

	public override void Added()
	{
		LocalBounds = new BoundingBox(Vec3.Zero, 3);

		// Setup auto-continue triggers
		for (int i = 0; i < PathNodes.Count; i++)
		{
			var node = PathNodes[i];
			if (!string.IsNullOrEmpty(node.AutoContinueTriggerID) &&
				AreaTrigger.GetAreaTrigger(World, node.AutoContinueTriggerID) is {} areaTrigger)
			{
				var thisIndex = i;
                void action(AreaTrigger trigger)
                {
					if (nodeIndex <= thisIndex)
					{
						MoveToNode(thisIndex + 1);
						tMoveWait = 0;
					}
					
                    trigger.RemoveEnterAction(action);
                }

				areaTrigger.AddEnterAction(action);
            }	
		}
	}

	public override void Update()
	{
		Particles.SpawnParticle(
			Position + new Vec3(6 - World.Rng.Float() * 12, 6 - World.Rng.Float() * 12, 6 - World.Rng.Float() * 12),
			new Vec3(0, 0, 0), 1, World.DeltaTime);
		Particles.Update(World.DeltaTime);

		if (tMoveWait > 0)
			tMoveWait -= World.DeltaTime;
		
		if (isMoving && tMoveWait <= 0)
		{
			Visible = true;
			if (moveEase < 1)
				moveEase += World.DeltaTime / MoveTime;

			Position = Vec3.Lerp(CurrentNode?.Position ?? Position, PathNodes[moveTargetIndex].Position, Ease.Quad.InOut(moveEase));

			// Spawn trails
			if (moveEase >= numTrails / 11f && numTrails < 11)
			{
				TrailSystem.CreateTrail(
					() => [new SkinnedModel(Assets.Models["refill_gem_double"])],
					Model.Transform * Matrix,
					0xff00ff
				);
				numTrails++;
			}

			if (moveEase >= 1)
			{
				isMoving = false;
				canCollect = true;
				nodeIndex = moveTargetIndex;
			}
		}
		TrailSystem.Update(World.DeltaTime);
	}

    public void CollectModels(List<(Actor Actor, Model Model)> populate)
	{
		Model.Transform =
			Matrix.CreateScale(2.0f) *
			Matrix.CreateTranslation(Vec3.UnitZ * MathF.Sin(World.GeneralTimer * 2.0f) * 2) *
			Matrix.CreateRotationZ(World.GeneralTimer * 3.0f);
		populate.Add((this, Model));

		List<Model> trailPopulate = [];
		TrailSystem.CollectModels(trailPopulate, Matrix);
		populate.AddRange(trailPopulate.Select<Model, (Actor, Model)>(model => (this, model)));
	}

    public void CollectSprites(List<Sprite> populate)
	{
		if (Visible)
			Particles.CollectSprites(Position, World, populate);
	}

	private void MoveToNode(int index)
	{
		// If there are no more nodes left, just stay invisible
		if (index >= PathNodes.Count)
		{
			canCollect = false;
			Visible = false;
			return;
		}

		canCollect = false;
		Visible = false;
		tMoveWait = 0.2f;
		isMoving = true;
		moveTargetIndex = index;
		moveEase = 0;
		numTrails = 0;
	}

    public void Pickup(Player player)
    {
		if (!canCollect) return;

        player.PurpleOrbLaunch(this);

		// Badeline stuff
		var badeline = new Badeline();
		World.Add(badeline);
		badeline.PurpleOrbLaunch(this, player.Facing, true);

		MoveToNode(nodeIndex + 1);
    }
}