
using Sledge.Formats.Tokens;

namespace Celeste64;

public class SnowWipe : ScreenWipe
{
	private const float Duration = 2;
	private const float SnowflakeFlyDuration = 0.4f;
	private const float MaxSnowflakeSpeed = 1.6f;
	private const int MaxSnowflakes = 300;
	private readonly Snowflake?[] snowflakes = new Snowflake[MaxSnowflakes];
	private int snowflakeAmount = 0;
	private float previousPercent = 0;
	
	private class Snowflake (Vec2 endPosition, int radius, Vec2 velocity, bool fromBlack = false)
	{
		public readonly Vec2 EndPosition = endPosition;
		// Not actual position, but values representing what percent of the bounds
		public Vec2 Position = endPosition - velocity * SnowflakeFlyDuration;
		public int Radius = radius;
		public Vec2 Velocity = velocity;
		public float Wobble => Stopped ? 0 : MathF.Sin(Time * 2 * MathF.PI / SnowflakeFlyDuration);
		public float Time = 0;
		public bool Stopped = fromBlack;
		public readonly bool FromBlack = fromBlack;

		public static Snowflake FromStartingPosition(Vec2 startPosition, int radius, Vec2 velocity, bool fromBlack = false)
		{
			return new Snowflake(startPosition + velocity * SnowflakeFlyDuration, radius, velocity, fromBlack);
		}

		public Vec2 GetActualPosition(Rect bounds)
		{
			// Don't need to add top left because there is a translation matrix that
			// should be in effect
			return Position * new Vec2(bounds.Width, bounds.Height);
		}

		public Vec2 GetActualVelocity(Rect bounds)
		{
			return Velocity * new Vec2(bounds.Width, bounds.Height);
		}

		// Before the snowflakes stop, make them get bigger gradually to give the illusion that they are getting close to the screen
		// After the snowflakes stop, gradually shrink them
		public float ActualRadius => FromBlack ?
			Radius * (SnowflakeFlyDuration - Time) / SnowflakeFlyDuration
			:
			Time >= SnowflakeFlyDuration ? Radius + Time / SnowflakeFlyDuration * 2 : Radius * Time / SnowflakeFlyDuration;

		public float Alpha => FromBlack ?
			Ease.Quad.Out(MathF.Max((SnowflakeFlyDuration - Time) / SnowflakeFlyDuration, 0))
			:
			Ease.Quad.Out(MathF.Min(Time / SnowflakeFlyDuration, 1));

		public void Update(float deltaTime)
		{
			if (!(Stopped && FromBlack)) Time += deltaTime;
			if (Stopped) return;
			
			Position += Velocity * deltaTime;
			if (Time >= SnowflakeFlyDuration) 
			{
				Time = SnowflakeFlyDuration;
				Stopped = true;
				Position = EndPosition;
			}
		}

		public void Render(Batcher batch, Rect bounds)
		{
			batch.Circle(GetActualPosition(bounds) + new Vec2(0, Wobble * bounds.Height / 40), ActualRadius, 12, Color.White * Alpha);
		}
	};

	public SnowWipe() : base(Duration)
	{
		CustomColor = Color.White;
	}

	public override void Start()
	{
        // TODO add snow/wind sound effect
		if (IsFromBlack)
		{
			Random rng = new();
			for (int i = 0; i < MaxSnowflakes; i++)
			{
				var velocity = new Vec2(-rng.NextSingle() * MaxSnowflakeSpeed, rng.NextSingle() * MaxSnowflakeSpeed) - new Vec2(0.1f, 0);	// Always down and to the left
				var position = new Vec2(rng.NextSingle() * 0.95f + 0.025f, rng.NextSingle() * 0.95f + 0.025f);
				snowflakes[i] = Snowflake.FromStartingPosition(position, (int)(rng.NextSingle() * 10) + 5, velocity, true);
			}
		}
		else 
		{
			for (int i = 0; i < MaxSnowflakes; i++)
			{
				snowflakes[i] = null;
			}
		}

		previousPercent = 0;
		snowflakeAmount = 0;
    }

	public override void Step(float percent)
	{
		// Add new snowflakes
		if (!IsFromBlack) {
			int newNumSnowflakes = Math.Min((int)(Ease.Cube.InOut(percent) * Duration / (Duration - SnowflakeFlyDuration) * MaxSnowflakes), MaxSnowflakes);
			Random rng = new();
			for (int i = snowflakeAmount; i < newNumSnowflakes; i++)
			{
				var velocity = new Vec2(-rng.NextSingle() * MaxSnowflakeSpeed, rng.NextSingle() * MaxSnowflakeSpeed) - new Vec2(0.1f, 0);	// Always down and to the left
				var position = new Vec2(rng.NextSingle() * 0.95f + 0.025f, rng.NextSingle() * 0.95f + 0.025f);
				snowflakes[i] = new Snowflake(position, (int)(rng.NextSingle() * 10) + 5, velocity);
			}
			snowflakeAmount = newNumSnowflakes;
		}

		// Let snowflakes start moving
		else
		{
			int newNumSnowflakes = Math.Min((int)(Ease.Cube.Out(percent) * Duration / (Duration - SnowflakeFlyDuration) * MaxSnowflakes), MaxSnowflakes);
			
			for (int i = snowflakeAmount; i < newNumSnowflakes; i++)
			{
				var it = snowflakes[i];
				if (it != null) it.Stopped = false;
			}
			snowflakeAmount = newNumSnowflakes;
		}
		
		// Update all the snowflakes
		foreach (var snowflake in snowflakes)
		{
			if (snowflake != null) snowflake.Update((percent - previousPercent) * Duration);
			// once we hit a null, the rest will also be null since it fills up from the beginning
			else break;
		}
		previousPercent = percent;
	}

	public override void Render(Batcher batch, Rect bounds)
	{
		if ((Percent <= 0 && IsFromBlack) || (Percent >= 1 && !IsFromBlack))
		{
			batch.Rect(bounds, Color.White);
			return;
		}

		batch.Rect(bounds, Color.White * (IsFromBlack ? Ease.Quint.In(1 - Percent) : Ease.Quint.In(Percent)));

		batch.PushMatrix(Matrix3x2.CreateTranslation(bounds.TopLeft));
		foreach (var snowflake in snowflakes) {
			if (snowflake != null) snowflake.Render(batch, bounds);
			// once we hit a null, the rest will also be null since it fills up from the beginning
			else break;
		}
		batch.PopMatrix();
	}
}