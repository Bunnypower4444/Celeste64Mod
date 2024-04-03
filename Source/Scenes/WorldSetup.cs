
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
            var badeline = world.Get<Badeline>();
            badeline?.SetDialogue(GetDialogueLines(BADELINE_DIALOG_KEYS_1));

            // Theo
            var theo = world.Get<Theo>();
            theo?.SetDialogue(GetDialogueLines(THEO_DIALOG_KEYS_1));

            // Granny
            var granny = world.Get<Granny>();
            granny?.SetDialogue(GetDialogueLines(GRANNY_DIALOG_KEYS_1));
        }
    }

    #endregion
}