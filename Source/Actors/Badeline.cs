
namespace Celeste64;

public class Badeline : NPC
{
	public const string TALK_FLAG = "BADELINE";

	private readonly Hair hair;
	private Color hairColor = 0x9B3FB5;

	private Routine routine = new();

    public Badeline() : base(Assets.Models["badeline"])
	{
		Model.Play("Bad.Idle");

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
	}

    public override void Update()
    {
        base.Update();
		
		// update model
		Model.Transform = 
			Matrix.CreateScale(3) * 
			Matrix.CreateTranslation(0, 0, MathF.Sin(World.GeneralTimer * 2) * 1.0f - 1.5f);

		// update hair
		{
			var hairMatrix = Matrix.Identity;
			foreach (var it in Model.Instance.Armature.LogicalNodes)
				if (it.Name == "Head")
					hairMatrix = it.ModelMatrix * SkinnedModel.BaseTranslation * Model.Transform * Matrix;
			hair.Flags = Model.Flags;
			hair.Forward = -new Vec3(Facing, 0);
			hair.Materials[0].Effects = 0;
			hair.Update(hairMatrix);
		}
		
		routine.Update();
    }

    public override void Interact(Player player)
	{
		World.Add(new Cutscene(Conversation));
	}

	private CoEnumerator Conversation(Cutscene cs)
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

	public void FuseWithMadeline()
	{
		var player = World.Get<Player>();
		if (player == null) return;

		routine.Run(FuseWithMadelineRoutine(player));
	}

	private CoEnumerator FuseWithMadelineRoutine(Player player)
	{
		float time = 0f;
		const float MoveTime = 1f;
		var origPos = Position;
		var origFacing = Facing;

		PushoutRadius = 0;

		while (time < MoveTime)
		{
			time += Time.Delta;
			var ease = Ease.Cube.InOut(time / MoveTime);
			Position = Vec3.Lerp(origPos, player.Position, ease);
			Facing = Vec2.Transform(origFacing, Matrix3x2.CreateRotation((player.Facing.Angle() - origFacing.Angle()) * ease));
			yield return Co.SingleFrame;
		}

		Visible = false;
		// create cool effect
		var refill = new Refill(true) { Visible = false, Position = player.Position };
		World.Add(refill);
		yield return Co.SingleFrame;
		refill.Pickup(player);
		player.Settings = player.Settings with { MaxDashes = 2 };

		yield return 0.4f;
		World.Destroy(refill);
	}
}

