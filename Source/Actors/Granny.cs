﻿
namespace Celeste64;

public class Granny : NPC
{
	public const string TALK_FLAG = "GRANNY";

	private List<Language.Line>?[] dialogue = [];

	public Granny() : base(Assets.Models["granny"])
	{
		Model.Transform = Matrix.CreateScale(3) * Matrix.CreateTranslation(0, 0, -1.5f);
		InteractHoverOffset = new Vec3(0, -2, 16);
		InteractRadius = 32;
		CheckForDialog();
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
		var lines = dialogue[Save.CurrentRecord.GetFlag(TALK_FLAG)] ?? throw new Exception("Null dialogue");
        yield return Co.Run(cs.Say(lines));
		Save.CurrentRecord.IncFlag(TALK_FLAG);
		CheckForDialog();
	}

	private void CheckForDialog()
	{ 
		var flag = Save.CurrentRecord.GetFlag(TALK_FLAG);
		InteractEnabled = flag < dialogue.Length && dialogue[flag] != null;
		// InteractEnabled = Loc.HasLines($"Granny{Save.CurrentRecord.GetFlag(TALK_FLAG) + 1}");
	}

	public void SetDialogue(List<Language.Line>?[] dialog)
	{
		dialogue = dialog;
		CheckForDialog();
	}
}
