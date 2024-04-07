
namespace Celeste64;

/// <summary>
/// Sets up stuff for each individual level (so that other classes don't have level-specific things in them), like cutscenes
/// </summary>
public static class WorldSetup
{
    public const string FORSAKEN_CITY = "1";

    public static void Setup(World world)
    {
        switch (world.Entry.Map)
        {
            case FORSAKEN_CITY:
                SetupForsakenCity(world);
                break;
        }
    }

    private static List<Language.Line>?[] GetDialogueLines(string?[] keys)
    {
        var dialogue = new List<Language.Line>?[keys.Length];
        int i = 0;
        foreach (var key in keys)
        {
            if (i >= keys.Length) break;
            if (key != null) dialogue[i] = Loc.Lines(key);
            i++;
        }
        return dialogue;
    }

    #region 1 - Forsaken City

    private static readonly string?[] BADELINE_DIALOG_KEYS_1 = ["Baddy1", "Baddy2", "Baddy3"];
    private static readonly string?[] THEO_DIALOG_KEYS_1 = ["Theo1", "Theo2", "Theo3"];
    private static readonly string?[] GRANNY_DIALOG_KEYS_1 = ["Granny1", "Granny2", "Granny3"];
    
    private static void SetupForsakenCity(World world)
    {
        // Ending area music
        {
            var endingArea = world.Get<EndingArea>();
            endingArea?.AddEnterAction(_ => {
                Game.Instance.Music.Set("at_baddy", 1);
            });
            endingArea?.AddExitAction(_ => {
                Game.Instance.Music.Set("at_baddy", 0);
            });
        }

        // Set character dialogue
        {
            // Badeline
            if (world.Get<Badeline>() is {} badeline) {
                badeline.SetDialogue(GetDialogueLines(BADELINE_DIALOG_KEYS_1));
                badeline.DialogueFinishActions.Add(bad => {
                    bad.DialogueIndex++;
                    Save.CurrentRecord.IncFlag(Badeline.TALK_FLAG);
                });
                badeline.DialogueIndex = Save.CurrentRecord.GetFlag(Badeline.TALK_FLAG);
            }

            // Theo
            if (world.Get<Theo>() is {} theo) {
                theo.SetDialogue(GetDialogueLines(THEO_DIALOG_KEYS_1));
                theo.DialogueFinishActions.Add(th => {
                    th.DialogueIndex++;
                    Save.CurrentRecord.IncFlag(Theo.TALK_FLAG);
                });
                theo.DialogueIndex = Save.CurrentRecord.GetFlag(Theo.TALK_FLAG);
            }

            // Granny
            if (world.Get<Granny>() is {} granny) {
                granny.SetDialogue(GetDialogueLines(GRANNY_DIALOG_KEYS_1));
                granny.DialogueFinishActions.Add(gr => {
                    gr.DialogueIndex++;
                    Save.CurrentRecord.IncFlag(Granny.TALK_FLAG);
                });
                granny.DialogueIndex = Save.CurrentRecord.GetFlag(Granny.TALK_FLAG);
            }
        }
    }

    #endregion
}