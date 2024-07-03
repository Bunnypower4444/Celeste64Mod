
namespace Celeste64;

public class Badeline : NPC
{
	public const string TALK_FLAG = "BADELINE";

	private enum States { Idle, Moving, Disappearing, PurpleOrbLaunch };
	private States state = States.Idle;
	private readonly Hair hair;
	private Color hairColor = 0x9B3FB5;
	private readonly Routine routine = new();
	private readonly ParticleSystem particleSystem = new(50, new ParticleTheme() {
		Rate = 1f,
		Sprite = "particle-star",
		Life = 0.5f,
		Gravity = new Vec3(0, 0, 0),
		Size = 2.5f,
	});

    public Badeline() : base(Assets.Models["badeline"])
	{
		foreach (var mat in Model.Materials)
		{
			if (mat.Name == "Hair")
			{
				mat.Color = hairColor;
				mat.Effects = 0;
			}
            mat.SilhouetteColor = hairColor;
		}

        hair = new()
        {
            Color = hairColor,
			ForwardOffsetPerNode = 0,
            Nodes = 10
        };

        InteractHoverOffset = new Vec3(0, -2, 16);
		InteractRadius = 32;
		DefaultPushoutRadius = PushoutRadius;
	}

    public override void Update()
    {
        base.Update();
		
		// update model
		if (state == States.Idle)
		{
			Model.Transform = 
			Matrix.CreateScale(3) * 
			Matrix.CreateTranslation(0, 0, MathF.Sin(World.GeneralTimer * 2) * 1.0f - 1.5f);
			Model.Play("Bad.Idle");
			InteractEnabled = true;
		}
		else
		{
			Model.Transform = Matrix.CreateScale(3);
			InteractEnabled = false;
		}

		if (state == States.Disappearing)
		{
			// Spawn a bunch of particles when she disappears
			for (int i = 0; i < particleSystem.MaxParticles; i++)
			{
				var magnitude = 6 - World.Rng.Float() * 12;
				particleSystem.SpawnParticle(
					Position +
						new Vec3(World.Rng.Float() * 2 - 1, World.Rng.Float() * 2 - 1, World.Rng.Float() * 2 - 1)
							.Normalized() * magnitude,
					new Vec3(0, 0, 0), 1 / World.DeltaTime, World.DeltaTime);
			}
			
			state = States.Idle;
		}

		particleSystem.Update(World.DeltaTime);

		if (state == States.PurpleOrbLaunch)
			StPurpleOrbLaunchUpdate();

		// update hair
		{
			var hairMatrix = Matrix.Identity;
			foreach (var it in Model.Instance.Armature.LogicalNodes)
				if (it.Name == "Head")
					hairMatrix = it.ModelMatrix * SkinnedModel.BaseTranslation * Model.Transform * Matrix;
			hair.Flags = Model.Flags;
			hair.Forward = -new Vec3(Facing, 0);
			hair.Materials[0].Effects = 0;
			hair.Update(hairMatrix, delta: World.DeltaTime);
		}
		
		if (routine.IsRunning)
			routine.Update(World.DeltaTime);
    }

    public override void Interact(Player player)
	{
		World.Add(new Cutscene(Conversation));
	}

	public CoEnumerator Conversation(Cutscene cs)
	{
		yield return Co.Run(cs.MoveToDistance(World.Get<Player>(), Position.XY(), 16));
		yield return Co.Run(cs.FaceEachOther(World.Get<Player>(), this));

		//int index = Save.CurrentRecord.GetFlag(TALK_FLAG) + 1;
		//yield return Co.Run(cs.Say(Loc.Lines($"Baddy{index}")));
		var lines = GetCurrentLine() ?? throw new Exception("Null dialogue");
		List<string> choices = [];
        yield return Co.Run(cs.Say(lines, choices));
		//Save.CurrentRecord.IncFlag(TALK_FLAG);
		RunDialogueActions(choices);
	}

    public override void CollectModels(List<(Actor Actor, Model Model)> populate)
    {
		populate.Add((this, hair));
        base.CollectModels(populate);
    }

    public override void CollectSprites(List<Sprite> populate)
    {
		particleSystem.CollectSprites(Position, World, populate);
        base.CollectSprites(populate);
    }

    public void FuseWithMadeline()
	{
		var player = World.Get<Player>();
		if (player == null) return;

		routine.Run(FuseWithMadelineRoutine(player));
	}

	private CoEnumerator FuseWithMadelineRoutine(Player player)
	{
		float time = 0f;
		const float MoveTime = 0.5f;
		var origPos = Position;
		var origFacing = Facing;

		PushoutRadius = 0;
		state = States.Moving;

		while (time < MoveTime)
		{
			time += World.DeltaTime;
			var ease = Ease.Quad.In(time / MoveTime);
			Position = Vec3.Lerp(origPos, player.Position, ease);
			Facing = Vec2.Transform(origFacing, Matrix3x2.CreateRotation((player.Facing.Angle() - origFacing.Angle()) * ease));
			yield return Co.SingleFrame;
		}

		MakeInvisible();
		// create cool effect
		var refill = new Refill(true) { Visible = false, Position = player.Position };
		World.Add(refill);
		yield return Co.SingleFrame;
		refill.Pickup(player);
		player.Settings = player.Settings with { MaxDashes = 2 };

		yield return 0.4f;
		World.Destroy(refill);
	}

	public void UnfuseFromMadeline(Vec3 direction)
	{
		var player = World.Get<Player>();
		if (player == null) return;

		MakeVisible();
		const float UnfuseDistance = 20f;
		routine.Run(UnfuseFromMadelineRoutine(player, player.Position + direction.Normalized() * UnfuseDistance));
	}

	private CoEnumerator UnfuseFromMadelineRoutine(Player player, Vec3 pos)
	{
		float time = 0f;
		const float MoveTime = 0.5f;
		var origPos = player.Position;
		var origFacing = player.Facing;
		var targetFacing = (pos - origPos).XY().Normalized();

		PushoutRadius = 0;
		state = States.Moving;
		player.Settings = player.Settings with { MaxDashes = 1 };

		while (time < MoveTime)
		{
			time += World.DeltaTime;
			var ease = Ease.Quad.Out(time / MoveTime);
			Position = Vec3.Lerp(origPos, pos, ease);
			Facing = Vec2.Transform(origFacing, Matrix3x2.CreateRotation((targetFacing.Angle() - origFacing.Angle()) * ease));
			yield return Co.SingleFrame;
		}

		state = States.Idle;
	}

	public void Disappear()
	{
		MakeInvisible();
		Audio.Play(Sfx.sfx_climb_ledge);
		state = States.Disappearing;
	}

	private PurpleOrb? currentPurpleOrb;
	private Vec2 purpleOrbDirection;
	private bool purpleOrbDestroy = false;
	private float tPurpleOrbLaunch = 0;

	public void PurpleOrbLaunch(PurpleOrb purpleOrb, Vec2 playerFacing, bool destroyAfter = false)
	{
		currentPurpleOrb = purpleOrb;
		purpleOrbDirection = playerFacing.Normalized();
		PushoutRadius = 0;
		Position = currentPurpleOrb.Position;
		Facing = -playerFacing;
		purpleOrbDestroy = destroyAfter;
		tPurpleOrbLaunch = 0.5f;
		state = States.PurpleOrbLaunch;
	}

	private void StPurpleOrbLaunchUpdate()
	{
		if (currentPurpleOrb == null) return;
		if (tPurpleOrbLaunch > 0)
			tPurpleOrbLaunch -= Time.Delta;
		else
		{
			MakeInvisible();
			if (purpleOrbDestroy)
				World.Destroy(this);
			state = States.Idle;
			currentPurpleOrb = null;
			return;
		}

		if (tPurpleOrbLaunch > 1)
			Position = Utils.Approach(Position, currentPurpleOrb.Position + new Vec3(purpleOrbDirection * 5, 0), 40 * World.DeltaTime);
	}

	private readonly float DefaultPushoutRadius;

	public void MakeInvisible()
	{
		Visible = false;
		InteractEnabled = false;
		PushoutRadius = 0;
	}

	public void MakeVisible()
	{
		Visible = true;
		InteractEnabled = true;
		PushoutRadius = DefaultPushoutRadius;
	}
}

