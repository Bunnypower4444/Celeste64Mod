
namespace Celeste64;

public class PacerStrawbUnlocker(int score) : Actor, IUnlockStrawberry
{
    public bool Satisfied => World.StartedPacerTest && World.PacerTestScore >= ScoreRequirement;
    public readonly int ScoreRequirement = score;
}