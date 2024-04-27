
namespace Celeste64;

public class GateBlock(Vec3 end, string unlockGroup = "") : Solid
{
	public const float Acceleration = 400;
	public const float MaxSpeed = 200;

	public Vec3 Start;
	public Vec3 End = end;
	private bool opened;
	private Sound? sfx;
	private readonly string unlockGroup = unlockGroup;
	private readonly List<Coin> unlockCoins = [];

	private readonly Routine routine = new();

    public override void Added()
    {
		sfx = World.Add(new Sound(this, Sfx.sfx_touch_switch_gate_open_move));
		UpdateOffScreen = true;
		Start = Position;
		if (unlockGroup != string.Empty)
		{
			foreach (var actor in World.All<Coin>())
			{
				if (actor.GroupName == unlockGroup)
					unlockCoins.Add((Coin)actor);
			}
		}
    }

	public override void Update()
	{
		base.Update();

		if (!opened)
		{
			if (unlockGroup == string.Empty)
			{
				if (!Coin.AnyRemaining(World))
				{
					opened = true;
					routine.Run(Sequence());
				}
			}
			else
			{
				if (!Coin.AnyRemaining(unlockCoins))
				{
					opened = true;
					routine.Run(Sequence());
				}
			}
		}
		else if (routine.IsRunning)
		{
			routine.Update(World.DeltaTime);
		}
	}

	private CoEnumerator Sequence()
	{
		TShake = .2f;
		sfx?.Resume();
		yield return .2f;

		var normal = (End - Position).Normalized();
		while (Position != End && Vec3.Dot((End - Position).Normalized(), normal) >= 0)
		{
			Velocity = Utils.Approach(Velocity, MaxSpeed * normal, Acceleration * World.DeltaTime);
			yield return Co.SingleFrame;
		}

		Audio.Play(Sfx.sfx_touch_switch_gate_finish, Position);
		sfx?.Stop();
		sfx = null;
		Velocity = Vec3.Zero;
		MoveTo(End);
		TShake = .2f;
	}

}
