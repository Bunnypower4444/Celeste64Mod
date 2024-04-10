
using System.Globalization;
using System.Xml;

namespace Celeste64;

public class Cutscene : Actor, IHaveUI
{
	public readonly Func<Cutscene, CoEnumerator> Running;
	public readonly Routine Routine;
	private float ease = 0.0f;

	private const float CharacterEaseInTime = 0.4f;
	private const float CharacterOffset = 1.0f / 40;

	// TODO add a table for colors so you can put the name of the color in the dialogue

	private struct Saying
	{
		public string Face;
		public string Text;
		public int Characters;
		public float Ease;
		public float Time;
		public Dictionary<int, float> Delays;
		public XmlDocument Document;
		public bool Talking;
		public float Delay;
	}
	private Saying saying;
	private AudioHandle dialogSnapshot;
	private float timer = 0;

	public bool FreezeGame = false;

	public Cutscene(Func<Cutscene, CoEnumerator> run, bool freezeEverythingExceptMe = false)
	{
		Running = run;
		Routine = new();
		FreezeGame = freezeEverythingExceptMe;
		UpdateOffScreen = true;
	}

	// Left angled bracket (<)
	private const string LeftTag = "left";
	// Right angled bracket (>)
	private const string RightTag = "right";
	// Waits for the text to finish easing in, then waits a certain duration specified by the "time" attribute
	private const string DelayTag = "d";
	// Sets the color of the text inside the tag to a hex color specified by the "color" attribute
	private const string ColorTag = "c";

    private static readonly Dictionary<string, string> ReplaceTags = new() {
        [LeftTag] = "<",
		[RightTag] = ">"
	};

	public static XmlDocument ParseDialogue(string line, out string text, out Dictionary<int, float> delays)
	{
		XmlDocument doc = new();
		doc.LoadXml($"<text>{line}</text>");	// Add root tag
		var _delays = new Dictionary<int, float>();
		var _text = "";
		
		void recurse(XmlNode node) {
			foreach (XmlNode child in node)
			{
				switch (child.NodeType)
				{
					case XmlNodeType.Text:
						_text += child.InnerText;
						break;

					case XmlNodeType.Element:
						if (ReplaceTags.TryGetValue(child.Name, out var str))
							_text += str;
						else if (child.Name == DelayTag && child.Attributes != null && child.Attributes["time"] is {} attr)
							if (float.TryParse(attr.Value, out var value))
							{
								if (_delays.ContainsKey(_text.Length)) _delays[_text.Length] += value;
								else _delays[_text.Length] = value;
							}
							else throw new Exception("Invalid float value for attribute 'time' in dialogue delay tag");
						break;
				}

				if (child.HasChildNodes)
				{
					recurse(child);
				}
			}
		}

		if (doc.ChildNodes[0] is {} node)	// so the compiler will shut up
			recurse(node);

		text = _text;
		delays = _delays;

		return doc;
		/*
		string result = "";
		actualTextLength = 0;
		delays = new float[line.Length];
		var inTag = false;
		var inTagStr = "";
		foreach (var ch in line)
		{
			if (!inTag && ch != '<')
			{
				result += ch;
				actualTextLength++;
			}
			else if (!inTag && ch == '<')
			{
				inTag = true;
				inTagStr = "<";
			}
			else if (inTag && ch != '>')
			{
				inTagStr += ch;
			}
			else if (inTag && ch == '>')
			{
				inTagStr += ch;
				var split = inTagStr[1..^1].Split(" ");
				var tagName = split[0];
				if (ReplaceTags.TryGetValue(tagName, out var value))
				{
					result += value;
					actualTextLength += value.Length;
				}
				else if (tagName == DelayTag)
				{
					delays[actualTextLength] = float.Parse(split[1]);
				}
				else
					result += inTagStr;
				inTag = false;
			}
		}

		if (inTag) throw new Exception("Unclosed tag in dialogue");

		return result;
		*/
	}

    public override void Destroyed()
    {
		Audio.StopBus(Sfx.bus_dialog, false);
		dialogSnapshot.Stop();
    }

    public CoEnumerator Say(List<Language.Line> lines)
	{
		foreach (var line in lines)
		{
			yield return Co.Run(Say(line.Face, line.Text, line.Voice));
		}

		Audio.StopBus(Sfx.bus_dialog, false);
	}

	public CoEnumerator Say(string face, string line, string? voice = null)
	{
		var doc = ParseDialogue(line, out var text, out var delays);
		saying = new Saying() {
			Face = $"faces/{face}",
			Text = text,
			Talking = false,
			Delays = delays,
			Document = doc
		};
		float totalTime = (saying.Text.Length - 1) * CharacterOffset + CharacterEaseInTime;
		if (delays.Count > 0)
			totalTime += delays.Values.Aggregate((acc, value) => acc + value);

		dialogSnapshot = Audio.Play(Sfx.snapshot_dialog);

		// ease in
		while (saying.Ease < 1.0f)
		{
			saying.Ease += Time.Delta * 10.0f;
			yield return Co.SingleFrame;
		}

		// start voice sound
		if (!string.IsNullOrEmpty(voice))
			Audio.Play($"event:/sfx/ui/dialog/{voice}");

		// print out dialog
		saying.Talking = saying.Text.Length > 3;
		var counter = 0.0f;
		while (saying.Characters < saying.Text.Length)
		{
			if (Controls.Confirm.Pressed || Controls.Cancel.Pressed)
			{
				saying.Characters = saying.Text.Length;
				saying.Time = totalTime;
				yield return Co.SingleFrame;
				break;
			}

			saying.Time += Time.Delta;
			var wasDelay = saying.Delay > 0;
			while (saying.Delay > 0)
			{
				saying.Delay -= Time.Delta;
				yield return Co.SingleFrame;
				saying.Time += Time.Delta;
			}
			if (wasDelay) saying.Characters++;

			counter += Time.Delta / CharacterOffset;
			while (counter >= 1)
			{
				saying.Characters++;
				counter -= 1;

				if (saying.Delays.TryGetValue(saying.Characters, out var d) && d > 0)
				{
					saying.Delay = d;
					saying.Characters--;
					break;
				}
			}

			yield return Co.SingleFrame;
		}

		// wait for last characters to ease in
		while (saying.Time < totalTime)
		{
			saying.Time += Time.Delta;
			if (Controls.Confirm.Pressed || Controls.Cancel.Pressed)
			{
				saying.Characters = saying.Text.Length;
				saying.Time = totalTime;
				yield return Co.SingleFrame;
				break;
			}

			yield return Co.SingleFrame;
		}

		// wait for confirm
		saying.Talking = false;
		while (!Controls.Confirm.Pressed && !Controls.Cancel.Pressed)
			yield return Co.SingleFrame;
		Audio.Play(Sfx.ui_dialog_advance);

		// ease out
		while (saying.Ease > 0.0f)
		{
			saying.Ease -= Time.Delta * 10.0f;
			yield return Co.SingleFrame;
		}
		
		dialogSnapshot.Stop();
		saying = new();
	}

	public CoEnumerator MoveToDistance(Actor? actor, Vec2 position, float distance)
	{
		if (actor != null)
		{
			var normal = (actor.Position.XY() - position).Normalized();
			yield return Co.Run(MoveTo(actor, position + normal * distance));
		}

		yield return Co.Continue;
	}

	public CoEnumerator MoveTo(Actor? actor, Vec2 position)
	{
		if (actor != null)
		{
			var player = actor as Player;

			if ((actor.Position.XY() - position).Length() > 4)
			{
				yield return Co.Run(Face(actor, new Vec3(position, 0)));

				player?.Model.Play("Run");
			}

			while (actor.Position.XY() != position)
			{
				var v2 = actor.Position.XY();
				Calc.Approach(ref v2, position, 50 * Time.Delta);
				actor.Position = actor.Position.WithXY(v2);
				yield return Co.SingleFrame;
			}

			player?.Model.Play("Idle");
		}
	}

	public CoEnumerator Face(Actor? actor, Vec3 target)
	{
		if (actor != null)
		{
			var facing = (target - actor.Position).XY().Normalized();
			var current = actor.Facing;

			while (MathF.Abs(facing.Angle() - current.Angle()) > 0.05f)
			{
				current = Calc.AngleToVector(Calc.AngleApproach(current.Angle(), facing.Angle(), MathF.Tau * 1.5f * Time.Delta));
				if (actor is Player player)
					player.SetTargetFacing(current);
				else
					actor.Facing = current;
				yield return Co.SingleFrame;
			}
		}

		yield return Co.Continue;
	}

	public CoEnumerator FaceEachOther(Actor? a0, Actor? a1)
	{
		if (a0 != null && a1 != null)
		{
			yield return Co.Run(Face(a0, a1.Position));
			yield return Co.Run(Face(a1, a0.Position));
		}
		yield return Co.Continue;
	}

	private CoEnumerator PerformCutscene()
	{
		Audio.Play(Sfx.sfx_readsign_in);

		while (ease < 1.0f)
		{
			Calc.Approach(ref ease, 1, Time.Delta * 10);
			yield return Co.SingleFrame;
		}

		yield return Co.Run(Running(this));

		Audio.Play(Sfx.sfx_readsign_out);

		while (ease < 1.0f)
		{
			Calc.Approach(ref ease, 0, Time.Delta * 10);
			yield return Co.SingleFrame;
		}
	}

	public override void Added()
	{
		Routine.Run(PerformCutscene());
	}

	public override void Update()
	{
		Routine.Update();
		timer += Time.Delta;

		if (!Routine.IsRunning)
			World.Destroy(this);
	}

	public void RenderUI(Batcher batch, Rect bounds)
	{
		const float BarSize = 40 * Game.RelativeScale;
		const float PortraitSize = 128 * Game.RelativeScale;
		const float TopOffset = 100 * Game.RelativeScale;
		const float EaseOffset = 32 * Game.RelativeScale;
		const float Padding = 8 * Game.RelativeScale;

		batch.Rect(new Rect(bounds.X, bounds.Y, bounds.Width, BarSize * ease), Color.Black);
		batch.Rect(new Rect(bounds.X, bounds.Bottom - BarSize * ease, bounds.Width, BarSize * ease), Color.Black);

		if (saying.Ease > 0 && !string.IsNullOrEmpty(saying.Text) && !World.Paused)
		{
			var ease = Ease.Cube.Out(saying.Ease);
			var font = Language.Current.SpriteFont;
			var size = font.SizeOf(saying.Text);
			var pos = bounds.TopCenter + new Vec2(0, TopOffset) - size / 2 - Vec2.One * Padding + new Vec2(0, EaseOffset * (1 - ease));

			Texture? face = null;

			// try to find taling face
			if (!string.IsNullOrEmpty(saying.Face))
			{
				var oddFrame = Time.BetweenInterval(timer, 0.3f, 0);
				var src = saying.Face;
				if (saying.Talking && Assets.Textures.ContainsKey($"{src}Talk00"))
				{
					if (oddFrame && Assets.Textures.ContainsKey($"{src}Talk01"))
						face = Assets.Textures[$"{src}Talk01"];
					else
						face = Assets.Textures[$"{src}Talk00"];
				}
				else if (!saying.Talking && Assets.Textures.ContainsKey($"{src}Idle00"))
				{
					// idle is blinking so hold on first frame for a long time, then 2nd frame for less time
					oddFrame = (timer % 3) > 2.8f;
					if (oddFrame && Assets.Textures.ContainsKey($"{src}Idle01"))
						face = Assets.Textures[$"{src}Idle01"];
					else
						face = Assets.Textures[$"{src}Idle00"];
				}
				else if (oddFrame && Assets.Textures.ContainsKey($"{src}01"))
				{
					face = Assets.Textures[$"{src}01"];
				}
				else if (Assets.Textures.ContainsKey($"{src}00"))
				{
					face = Assets.Textures[$"{src}00"];
				}
				else
				{
					face = Assets.Textures.GetValueOrDefault(src);
				}
			}

			if (face != null)
				pos.X += PortraitSize / 3;

			var box = new Rect(pos.X, pos.Y, size.X + Padding * 2, size.Y + Padding * 2);
			batch.RectRounded(box + new Vec2(0, 1), 4, Color.Black);
			batch.RectRounded(box, 4, Color.White);

			if (face != null)
			{
				var faceBox = new Rect(pos.X - PortraitSize * 0.8f, pos.Y + box.Height / 2 - PortraitSize / 2 - 10, PortraitSize, PortraitSize);
				batch.ImageFit(new Subtexture(face), faceBox, Vec2.One * 0.5f, Color.White, false, false);
			}

            // Draw the letters
            RenderText(batch, font, pos + new Vec2(Padding, Padding), saying);
		}
	}

	private static void RenderText(Batcher batch, SpriteFont font, Vec2 pos, Saying saying)
	{
		var origX = pos.X;
		var i = 0;
		var timeOffset = 0f;
		Stack<Color> colors = [];
		colors.Push(Color.Black);

		void recurse(XmlNode node)
		{
			foreach (XmlNode child in node)
			{
				bool pushedColor = false;
				switch (child.NodeType)
				{
					case XmlNodeType.Text:
						foreach (var _ in child.InnerText)
						{
							if (i >= saying.Characters) return;
							if (saying.Delays.TryGetValue(i, out var d))
								timeOffset += d;

							if (saying.Text[i] == '\n')
							{
								pos.X = origX;
								pos.Y += font.LineHeight;
							}
							else
							{								
								var charEase = Ease.Quart.In(1 - Calc.Clamp((saying.Time - i * CharacterOffset - timeOffset) / CharacterEaseInTime));
								batch.Text(font, saying.Text[i] + "", pos - new Vec2(0, charEase * font.LineHeight * 0.4f), colors.Peek() * (1 - charEase));
								pos.X += font.WidthOf(saying.Text[i] + "");
							}
							i++;
						}
						break;

					case XmlNodeType.Element:
						if (child.Name == ColorTag && child.Attributes != null && child.Attributes["color"] is {} attr)
						{
							if (int.TryParse(attr.Value, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var result))
							{
								colors.Push(result);
								pushedColor = true;
							}
							else throw new Exception("Invalid hexadecimal value for attribute 'color' in dialogue color tag");
						}
						break;
				}

				if (child.HasChildNodes)
					recurse(child);

				if (pushedColor)
					colors.Pop();
			}
		}
		recurse(saying.Document);
		/*
		for (int i = 0; i < saying.Characters; i++)
		{
			if (saying.Text[i] == '\n')
			{
				pos.X = origX;
				pos.Y += font.LineHeight;
			}
			else
			{
				var charEase = Ease.Quart.In(1 - Calc.Clamp((saying.Time - i * CharacterOffset) / CharacterEaseInTime));
				batch.Text(font, saying.Text[i] + "", pos - new Vec2(0, charEase * font.LineHeight * 0.4f), Color.Black * (1 - charEase));
				pos.X += font.WidthOf(saying.Text[i] + "");
			}
		}
		*/
		// batch.Text(font, saying.Text.AsSpan(0, saying.Characters), pos + new Vec2(Padding, Padding), Color.Black);
	}
}
