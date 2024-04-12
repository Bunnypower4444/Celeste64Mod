
namespace Celeste64;

public class Signpost : NPC, IHaveModels
{
	public readonly string Conversation;

	public Signpost(string conversation) : base(Assets.Models["sign"])
	{
		Conversation = conversation;
		Model.Transform = 
			Matrix.CreateScale(4) *
			Matrix.CreateTranslation(0, 0, -1.5f);
		InteractHoverOffset = new Vec3(0, 0, 16);
		InteractRadius = 16;
		PushoutRadius = 6;
		SetDialogue([Loc.Lines(Conversation)]);
	}

    public override void Interact(Player player)
	{
		World.Add(new Cutscene(Talk));
	}

	private CoEnumerator Talk(Cutscene cs)
	{
		yield return Co.Run(cs.Face(World.Get<Player>(), Position));
		List<string> choices = [];
		yield return Co.Run(cs.Say(GetCurrentLine() ?? throw new Exception("Null dialogue"), choices));
		RunDialogueActions(choices);
	}
}
