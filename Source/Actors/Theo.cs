
namespace Celeste64;

public class Theo : NPC
{
	public const string TALK_FLAG = "THEO";
	
	public Theo() : base(Assets.Models["theo"])
	{
		Model.Transform = Matrix.CreateScale(3) * Matrix.CreateTranslation(0, 0, -1.5f);
		InteractHoverOffset = new Vec3(0, -2, 16);
		InteractRadius = 32;
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
}

