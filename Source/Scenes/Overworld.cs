
namespace Celeste64;

public class Overworld : Scene
{
	public const int CardWidth = (int)(480 * Game.RelativeScale);
	public const int CardHeight = (int)(320 * Game.RelativeScale);

	public class Entry
	{
		public enum MenuState {
			Start, ASides, BSides, CSides
		}
		public readonly LevelInfo Level;
		public readonly Target Target;
		public readonly Subtexture Image;
		public readonly Menu Menu;
		public readonly List<Menu>? ASides;
		public int currentASideMenuIndex = 0;
		public string? SelectedCheckpoint;
		public readonly List<Menu>? BSides;
		public int currentBSideMenuIndex = 0;
		public string? SelectedSubmap;
		public readonly List<Menu>? CSides;
		public int currentCSideMenuIndex = 0;
		public MenuState CurrentMenuState;
		public readonly bool Complete = false;

		public float HighlightEase;
		public float SelectionEase;

		public Entry(LevelInfo level)
		{
			Level = level;
			Target = new Target(CardWidth, CardHeight);
			Image = new(Assets.Textures[level.Preview]);
			Menu = new();
			Menu.UpSound = Sfx.main_menu_roll_up;
			Menu.DownSound = Sfx.main_menu_roll_down;

			if (Save.Instance.TryGetRecord(Level.ID) is {} record)
			{
				var str = Loc.Str("Continue");
				if (record.Checkpoint != string.Empty && record.Checkpoint != Map.StartCheckpoint) {
					str += $" ({record.Checkpoint})";
				}
				Menu.Add(new Menu.Option(str));
				Menu.Add(new Menu.Option(Loc.Str("Restart")));
				Complete = record.Strawberries.Count >= Level.Strawberries;

				// Checkpoints (also includes start but only appears once you have one other checkpoint)
				if (record.ReachedCheckpoints.Count > 0) 
				{
					ASides = [];
					int checkpointIndex = 0, checkpointCount = 0;
					for (int i = 0; i < record.ReachedCheckpoints.Count / 4.0f; i++)
					{	
						ASides.Add(new() {
							UpSound = Sfx.main_menu_roll_up,
							DownSound = Sfx.main_menu_roll_down,
							Focused = false
						});
						if (i == 0) ASides[i].Focused = true;
						var toArray = Assets.Maps[Level.Map]?.Checkpoints.ToArray();
						for ( ; checkpointCount < (i + 1) * 4 && checkpointIndex < toArray?.Length; checkpointIndex++) {
							var checkpoint = toArray is not null ? toArray[checkpointIndex] : string.Empty;
							if (record.ReachedCheckpoints.Contains(checkpoint) || checkpoint == Map.StartCheckpoint)
							{
								ASides[i].Add(new Menu.Option(checkpoint));
								checkpointCount++;
							}
						}
					}
				}

				// B-Sides (Submaps)
				if (record.CompletedSubMaps.Count > 0)
				{
					BSides = [];
					int submapIndex = 0, submapCount = 0;
					HashSet<string> unlockedMaps = new(record.CompletedSubMaps.Union(record.StartedSubMaps));
					for (int i = 0; i < unlockedMaps.Count / 4.0f; i++)
					{	
						BSides.Add(new() {
							UpSound = Sfx.main_menu_roll_up,
							DownSound = Sfx.main_menu_roll_down,
							Focused = false
						});
						if (i == 0) BSides[i].Focused = true;
						// If there is an order to the submaps, use that
						var toArray = Level.SubmapOrder ?? Assets.Maps[Level.Map]?.Submaps.ToArray();
						for ( ; submapCount < (i + 1) * 4 && submapIndex < toArray?.Length; submapIndex++) {
							var submap = toArray is not null ? toArray[submapIndex] : string.Empty;
							if (unlockedMaps.Contains(submap) && submap != string.Empty)
							{
								BSides[i].Add(new Menu.Option(submap + (!record.CompletedSubMaps.Contains(submap) ? " (!)" : "")));
								submapCount++;
							}
						}
					}
				}

				// C-Side menu, make sure the c-side exists
				if (record.CSideUnlocked && Assets.Maps.TryGetValue(Level.CSideMap, out var csideMap))
				{
					CSides = [];
					int checkpointIndex = 0, checkpointCount = 0;
					// Add 2 for "Continue" option and the Start checkpoint
					int numCheckpoints = record.ReachedCSideCheckpoints.Count + 2;
					for (int i = 0; i < numCheckpoints / 4.0f; i++)
					{	
						CSides.Add(new() {
							UpSound = Sfx.main_menu_roll_up,
							DownSound = Sfx.main_menu_roll_down,
							Focused = false
						});
						if (i == 0) CSides[i].Focused = true;
						var toArray = csideMap.Checkpoints.ToArray();
						for ( ; checkpointCount < (i + 1) * 4 && checkpointIndex < toArray?.Length; checkpointIndex++) {
							// Continue button
							if (checkpointCount == 0)
							{
								var continueStr = Loc.Str("Continue");
								if (record.CSideCheckpoint != string.Empty && record.CSideCheckpoint != Map.StartCheckpoint)
									continueStr += $" ({record.CSideCheckpoint})";
								
								CSides[i].Add(new Menu.Option(continueStr));
								checkpointIndex--;
								checkpointCount++;
								continue;
							}

							var checkpoint = toArray is not null ? toArray[checkpointIndex] : string.Empty;
							if (record.ReachedCSideCheckpoints.Contains(checkpoint) || checkpoint == Map.StartCheckpoint)
							{
								CSides[i].Add(new Menu.Option(checkpoint));
								checkpointCount++;
							}
						}
					}
				}
			}
			else
			{
				Menu.Add(new Menu.Option(Loc.Str("Start")));
			}
		}

		public void Redraw(Batcher batch, float shine)
		{
			const float Padding = 16 * Game.RelativeScale;

			Target.Clear(Color.Transparent);
			batch.SetSampler(new(TextureFilter.Linear, TextureWrap.ClampToEdge, TextureWrap.ClampToEdge));

			var bounds = Target.Bounds;
			var font = Language.Current.SpriteFont;
			var img = (SelectionEase < 0.50f ? Image : new(Assets.Textures["postcards/back"]));

			if (img.Texture != null)
			{
				var scale = MathF.Max(bounds.Width / img.Width, bounds.Height / img.Height);
				var size = new Vec2(img.Width, img.Height);
				batch.Image(img, bounds.Center, size / 2, Vec2.One * scale, 0, Color.White);
			}

			if (SelectionEase < 0.50f)
			{
				if (Complete && Assets.Textures.GetValueOrDefault("overworld/strawberry") is {} texture)
				{
					batch.Image(
						new Subtexture(texture), 
						bounds.BottomRight - new Vec2(50, 0), 
						new Vec2(texture.Width / 2, texture.Height), 
						Vec2.One * 0.50f, 0, Color.White);
				}

				batch.PushMatrix(Matrix3x2.CreateScale(2.0f) * Matrix3x2.CreateTranslation(bounds.BottomLeft + new Vec2(Padding, -Padding)));
				UI.Text(batch, Level.Name, Vec2.Zero, new Vec2(0, 1), Color.White);
				batch.PopMatrix();
			}
			else
			{
				batch.Rect(bounds, Color.Black * 0.25f);

				// info
				batch.Text(font, Level.Label, bounds.TopLeft + new Vec2(32, 24) * Game.RelativeScale, Color.Black * 0.7f);

				// stats
				var yTranslation = CurrentMenuState != MenuState.Start ? bounds.Center - new Vec2(0, bounds.Center.Y / 6) : bounds.Center;
				batch.PushMatrix(Matrix3x2.CreateScale(1.3f) * Matrix3x2.CreateTranslation(yTranslation + new Vec2(0, -Padding)));
				{
					int strawbs = 0, deaths = 0;
					TimeSpan time = new();

					if (Save.Instance.TryGetRecord(Level.ID) is { } record)
					{
						strawbs = record.Strawberries.Count;
						deaths = record.Deaths;
						time = record.Time;
					}

					UI.Strawberries(batch, strawbs, new Vec2(-8, -UI.IconSize / 2 - 4), 1);
					UI.Deaths(batch, deaths, new Vec2(8, -UI.IconSize / 2 - 4), 0);
					UI.Timer(batch, time, new Vec2(0, UI.IconSize / 2 + 4), 0.5f);
				}
				batch.PopMatrix();

				// Checkpoints / B-sides
				switch (CurrentMenuState) {
					case MenuState.Start:
						// options
						Menu.Render(batch, bounds.BottomCenter + new Vec2(0, -Menu.Size.Y - 8));
						if (ASides != null) 
							UI.Prompt(batch, Controls.Left, Loc.Str("Checkpoints"), bounds.BottomLeft + new Vec2(4, -UI.PromptSize - 8), out _, 0);
						else if (BSides != null)
							UI.Prompt(batch, Controls.Left, Loc.Str("B-Sides"), bounds.BottomLeft + new Vec2(4, -UI.PromptSize - 8), out _, 0);
						else if (CSides != null)
							UI.Prompt(batch, Controls.Left, Loc.Str("C-Sides"), bounds.BottomLeft + new Vec2(4, -UI.PromptSize - 8), out _, 0);
						break;

					case MenuState.ASides:
						if (ASides is null) break;
						for (int i = 0; i < ASides.Count; i++) {
							var menu = ASides[i];
							menu.Render(batch, new Vec2((i + 0.5f) * bounds.Width / ASides.Count, bounds.Center.Y + bounds.Center.Y / 5 + menu.Size.Y / 2));
						}

						batch.PushMatrix(Matrix3x2.CreateScale(1.2f) * Matrix3x2.CreateTranslation(bounds.BottomCenter + new Vec2(0, -8)));
						UI.Text(batch, Loc.Str("Checkpoints"), Vec2.Zero, new Vec2(0.5f, 1), Color.White);
						batch.PopMatrix();

						UI.Prompt(batch, Controls.Right, Loc.Str("Continue"), bounds.BottomRight + new Vec2(-8, -UI.PromptSize - 8), out _, 1);
						if (BSides != null)
							UI.Prompt(batch, Controls.Left, Loc.Str("B-Sides"), bounds.BottomLeft + new Vec2(4, -UI.PromptSize - 8), out _, 0);
						else if (CSides != null)
							UI.Prompt(batch, Controls.Left, Loc.Str("C-Sides"), bounds.BottomLeft + new Vec2(4, -UI.PromptSize - 8), out _, 0);
						break;

					case MenuState.BSides:
						if (BSides is null) break;
						for (int i = 0; i < BSides.Count; i++) {
							var menu = BSides[i];
							menu.Render(batch, new Vec2((i + 0.5f) * bounds.Width / BSides.Count, bounds.Center.Y + bounds.Center.Y / 5 + menu.Size.Y / 2));
						}
						
						batch.PushMatrix(Matrix3x2.CreateScale(1.2f) * Matrix3x2.CreateTranslation(bounds.BottomCenter + new Vec2(0, -8)));
						UI.Text(batch, Loc.Str("B-Sides"), Vec2.Zero, new Vec2(0.5f, 1), Color.White);
						batch.PopMatrix();

						if (ASides is not null)
							UI.Prompt(batch, Controls.Right, Loc.Str("Checkpoints"), bounds.BottomRight + new Vec2(-8, -UI.PromptSize - 8), out _, 1);
						else UI.Prompt(batch, Controls.Right, Loc.Str("Continue"), bounds.BottomRight + new Vec2(-8, -UI.PromptSize - 8), out _, 1);

						if (CSides != null)
							UI.Prompt(batch, Controls.Left, Loc.Str("C-Side"), bounds.BottomLeft + new Vec2(4, -UI.PromptSize - 8), out _, 0);
						break;

					case MenuState.CSides:
						if (CSides is null) break;
						for (int i = 0; i < CSides.Count; i++) {
							var menu = CSides[i];
							menu.Render(batch, new Vec2((i + 0.5f) * bounds.Width / CSides.Count, bounds.Center.Y + bounds.Center.Y / 5 + menu.Size.Y / 2));
						}

						batch.PushMatrix(Matrix3x2.CreateScale(1.2f) * Matrix3x2.CreateTranslation(bounds.BottomCenter + new Vec2(0, -8)));
						UI.Text(batch, Loc.Str("C-Side"), Vec2.Zero, new Vec2(0.5f, 1), Color.White);
						batch.PopMatrix();

						if (BSides != null)
							UI.Prompt(batch, Controls.Right, Loc.Str("B-Sides"), bounds.BottomRight + new Vec2(-8, -UI.PromptSize - 8), out _, 1);
						else if (ASides != null)
							UI.Prompt(batch, Controls.Right, Loc.Str("Checkpoints"), bounds.BottomRight + new Vec2(-8, -UI.PromptSize - 8), out _, 1);
						else
							UI.Prompt(batch, Controls.Right, Loc.Str("Continue"), bounds.BottomRight + new Vec2(-8, -UI.PromptSize - 8), out _, 1);
						break;
				}
			}

			if (shine > 0)
			{
				batch.Line(
					bounds.BottomLeft + new Vec2(-50 + shine * 50, 50), 
					bounds.TopCenter + new Vec2(shine * 50, -50), 120, 
					Color.White * shine * 0.30f);

				batch.Line(
					bounds.BottomLeft + new Vec2(-50 + 100 + shine * 120, 50), 
					bounds.TopCenter + new Vec2(100 + shine * 120, -50), 70, 
					Color.White * shine * 0.30f);
			}

			batch.Render(Target);
		}
	}

	private enum States
	{
		Selecting,
		Selected,
		Entering,
		EnteringCheckpoint,
		EnteringSubmap,
		EntertingCSide,
		Restarting
	}

	private States state = States.Selecting;
	private bool IsEntering => state == States.Entering|| state == States.EnteringCheckpoint ||
		state == States.EnteringSubmap || state == States.EntertingCSide;
	private int index = 0;
	private float slide = 0;
	private float selectedEase = 0;
	private float cameraCloseUpEase = 0;
	private Vec2 wobble = new Vec2(0, -1);
	private readonly List<Entry> entries = [];
	private Entry currentEntry => entries[index];
	private readonly Batcher batch = new();
	private readonly Mesh mesh = new();
	private readonly Material material = new(Assets.Shaders["Sprite"]);
	private readonly Menu restartConfirmMenu = new();

	public Overworld(bool startOnLastSelected)
	{
		Music = "event:/music/mus_title";
		
		foreach (var level in Assets.Levels)
			entries.Add(new(level));

		var cardWidth = CardWidth / 6.0f / Game.RelativeScale;
		var cardHeight = CardHeight / 6.0f / Game.RelativeScale;

		mesh.SetVertices<SpriteVertex>([
			new(new Vec3(-cardWidth, 0, -cardHeight) / 2, new Vec2(0, 0), Color.White),
			new(new Vec3(cardWidth, 0, -cardHeight) / 2, new Vec2(1, 0), Color.White),
			new(new Vec3(cardWidth, 0, cardHeight) / 2, new Vec2(1, 1), Color.White),
			new(new Vec3(-cardWidth, 0, cardHeight) / 2, new Vec2(0, 1), Color.White),
		]);
		mesh.SetIndices<int>([0, 1, 2, 0, 2, 3]);

		restartConfirmMenu.Add(new Menu.Option(Loc.Str("Cancel")));
		restartConfirmMenu.Add(new Menu.Option(Loc.Str("RestartLevel")));
		restartConfirmMenu.UpSound = Sfx.main_menu_roll_up;
		restartConfirmMenu.DownSound = Sfx.main_menu_roll_down;

		if (startOnLastSelected)
		{
			var exitedFrom = entries.FindIndex((e) => e.Level.ID == Save.Instance.LevelID);
			if (exitedFrom >= 0)
			{
				index = exitedFrom;
				state = States.Selected;
				selectedEase = 1.0f;
				currentEntry.HighlightEase = currentEntry.SelectionEase = 1.0f;
			}
		}

		cameraCloseUpEase = 1.0f;
	}

	public override void Update()
	{
		slide += (index - slide) * (1 - MathF.Pow(.001f, Time.Delta));
		wobble += (Controls.Camera.Value - wobble) * (1 - MathF.Pow(.1f, Time.Delta));
		Calc.Approach(ref cameraCloseUpEase, IsEntering ? 1 : 0, Time.Delta);
		Calc.Approach(ref selectedEase, state != States.Selecting ? 1 : 0, 8 * Time.Delta);

		for (int i = 0; i < entries.Count; i++)
		{
			var it = entries[i];
			Calc.Approach(ref it.HighlightEase, index == i ? 1.0f : 0.0f, Time.Delta * 8.0f); 
			Calc.Approach(ref it.SelectionEase, index == i && (state == States.Selected || state == States.Restarting) ? 1.0f : 0.0f, Time.Delta * 4.0f);

			if (it.SelectionEase >= 0.50f && state == States.Selected)
			{
				switch (it.CurrentMenuState) {
					case Entry.MenuState.Start:
						it.Menu.Update();
						break;

					case Entry.MenuState.ASides:
						if (it.ASides is null) break;
						foreach (var menu in it.ASides) {
							menu.Update();
						}
						break;

					case Entry.MenuState.BSides:
						if (it.BSides is null) break;
						foreach (var menu in it.BSides) {
							menu.Update();
						}
						break;
						
					case Entry.MenuState.CSides:
						if (it.CSides == null) break;
						foreach (var menu in it.CSides) {
							menu.Update();
						}
						break;
				}
			}
			it.Menu.Focused = state == States.Selected;
		}

		if (Game.Instance.IsMidTransition)
			return;
		
		if (state == States.Selecting)
		{
			var was = index;
			if (Controls.Menu.Horizontal.Negative.Pressed)
			{
				Controls.Menu.ConsumePress();
				index--;
			}
			if (Controls.Menu.Horizontal.Positive.Pressed)
			{
				Controls.Menu.ConsumePress();
				index++;
			}
			index = Calc.Clamp(index, 0, entries.Count - 1);

			if (was != index)
				Audio.Play(Sfx.ui_move);

			if (Controls.Confirm.ConsumePress())
			{
				state = States.Selected;
				currentEntry.Menu.Index = 0;
				Audio.Play(Sfx.main_menu_postcard_flip);
			}

			if (Controls.Cancel.ConsumePress())
			{
				Game.Instance.Goto(new Transition()
				{
					Mode = Transition.Modes.Replace,
					Scene = () => new Titlescreen(),
					ToBlack = new AngledWipe(),
					ToPause = true
				});
			}
		}
		else if (state == States.Selected)
		{
			if (Controls.Confirm.ConsumePress() && currentEntry.SelectionEase > 0.50f)
			{
				switch (currentEntry.CurrentMenuState) {
					case Entry.MenuState.Start:
						if (currentEntry.Menu.Index == 1)
						{
							Audio.Play(Sfx.main_menu_restart_confirm_popup);
							restartConfirmMenu.Index = 0;
							state = States.Restarting;
						}
						else
						{
							Audio.Play(Sfx.main_menu_start_game);
							Game.Instance.Music.Stop();
							state = States.Entering;
						}
						break;
					
					case Entry.MenuState.ASides:
						Audio.Play(Sfx.main_menu_start_game);
						Game.Instance.Music.Stop();
						state = States.EnteringCheckpoint;
						currentEntry.SelectedCheckpoint = currentEntry.ASides?[currentEntry.currentASideMenuIndex].CurrentItem.Label;
						break;
					
					case Entry.MenuState.BSides:
						Audio.Play(Sfx.main_menu_start_game);
						Game.Instance.Music.Stop();
						state = States.EnteringSubmap;
						currentEntry.SelectedSubmap = currentEntry.BSides?[currentEntry.currentBSideMenuIndex].CurrentItem.Label;
						// remove incomplete submap alert
						if (currentEntry.SelectedSubmap?.EndsWith(" (!)") ?? false)
							currentEntry.SelectedSubmap = currentEntry.SelectedSubmap[0 .. ^4];
						break;

					case Entry.MenuState.CSides:
						// Make sure the c-side exists
						if (!Assets.Maps.TryGetValue(currentEntry.Level.CSideMap, out _)) break;
						Audio.Play(Sfx.main_menu_start_game);
						Game.Instance.Music.Stop();
						state = States.EntertingCSide;
						currentEntry.SelectedCheckpoint = currentEntry.CSides?[currentEntry.currentCSideMenuIndex].CurrentItem.Label;
						if (currentEntry.SelectedCheckpoint?.StartsWith("Continue") ?? false)
						{
							if (currentEntry.SelectedCheckpoint == "Continue")
								currentEntry.SelectedCheckpoint = Map.StartCheckpoint;
							// Get the checkpoint name from the parenthesis
							else
								currentEntry.SelectedCheckpoint = currentEntry.SelectedCheckpoint[10 .. ^1];
						}
						break;
				}
			}
			else if (Controls.Cancel.ConsumePress())
			{
				Audio.Play(Sfx.main_menu_postcard_flip_back);
				state = States.Selecting;
			}
			else if (Controls.Left.ConsumePress()) {
				if (currentEntry.CurrentMenuState == Entry.MenuState.Start)
				{	
					if (currentEntry.ASides is not null) 
					{
						Audio.Play(Sfx.ui_move);
						currentEntry.CurrentMenuState = Entry.MenuState.ASides;
					}
					else if (currentEntry.BSides is not null)
					{
						Audio.Play(Sfx.ui_move);
						currentEntry.CurrentMenuState = Entry.MenuState.BSides;
					}
					else if (currentEntry.CSides is not null)
					{
						Audio.Play(Sfx.ui_move);
						currentEntry.CurrentMenuState = Entry.MenuState.CSides;
					}
				}
				else if (currentEntry.CurrentMenuState == Entry.MenuState.ASides)
				{
					if (currentEntry.BSides is not null)
					{
						Audio.Play(Sfx.ui_move);
						currentEntry.CurrentMenuState = Entry.MenuState.BSides;
					}
					else if (currentEntry.CSides is not null)
					{
						Audio.Play(Sfx.ui_move);
						currentEntry.CurrentMenuState = Entry.MenuState.CSides;
					}
				}
				else if (currentEntry.CurrentMenuState == Entry.MenuState.BSides)
				{
					if (currentEntry.CSides != null)
					{
						Audio.Play(Sfx.ui_move);
						currentEntry.CurrentMenuState = Entry.MenuState.CSides;
					}
				}
			}
			else if (Controls.Right.ConsumePress()) {
				if (currentEntry.CurrentMenuState == Entry.MenuState.ASides) 
				{
					Audio.Play(Sfx.ui_move);
					currentEntry.CurrentMenuState = Entry.MenuState.Start;
				}
				else if (currentEntry.CurrentMenuState == Entry.MenuState.BSides)
				{
					Audio.Play(Sfx.ui_move);
					if (currentEntry.ASides is not null) currentEntry.CurrentMenuState = Entry.MenuState.ASides;
					else currentEntry.CurrentMenuState = Entry.MenuState.Start;
				}
				else if (currentEntry.CurrentMenuState == Entry.MenuState.CSides)
				{
					Audio.Play(Sfx.ui_move);
					if (currentEntry.BSides != null) currentEntry.CurrentMenuState = Entry.MenuState.BSides;
					else if (currentEntry.ASides != null) currentEntry.CurrentMenuState = Entry.MenuState.ASides;
					else currentEntry.CurrentMenuState = Entry.MenuState.Start;
				}
			}
			else if (Controls.Menu.Horizontal.Positive.Pressed) {
				if (currentEntry.CurrentMenuState == Entry.MenuState.ASides && currentEntry.ASides is not null ||
					 currentEntry.CurrentMenuState == Entry.MenuState.BSides && currentEntry.BSides is not null ||
					 currentEntry.CurrentMenuState == Entry.MenuState.CSides && currentEntry.CSides is not null) 
				{
					List<Menu>? menuList = null;
					switch (currentEntry.CurrentMenuState)
					{
						case Entry.MenuState.ASides:
							menuList = currentEntry.ASides;
							break;
						case Entry.MenuState.BSides:
							menuList = currentEntry.BSides;
							break;
						case Entry.MenuState.CSides:
							menuList = currentEntry.CSides;
							break;
					}
					if (menuList is null) goto End;
					int menuIndex () => currentEntry.CurrentMenuState switch
					{
						Entry.MenuState.BSides => currentEntry.currentBSideMenuIndex,
						Entry.MenuState.CSides => currentEntry.currentCSideMenuIndex,
						_ => currentEntry.currentASideMenuIndex
					};

                    void setMenuIndex (int value) {
						switch (currentEntry.CurrentMenuState)
						{
							case Entry.MenuState.BSides: currentEntry.currentBSideMenuIndex = value; break;
							case Entry.MenuState.CSides: currentEntry.currentCSideMenuIndex = value; break;
							default: currentEntry.currentASideMenuIndex = value; break;
						}
					}

					if (menuIndex() >= menuList.Count - 1) goto End;
					
					menuList[menuIndex()].Focused = false;
					var ind = menuList[menuIndex()].Index;
					setMenuIndex(menuIndex() + 1);
					Audio.Play(Sfx.main_menu_roll_up);

					menuList[menuIndex()].Focused = true;
					menuList[menuIndex()].Index = Math.Min(ind, menuList[menuIndex()].Count - 1);

					End: {}
				}
			}
			else if (Controls.Menu.Horizontal.Negative.Pressed) {
				if (currentEntry.CurrentMenuState == Entry.MenuState.ASides && currentEntry.ASides is not null ||
					 currentEntry.CurrentMenuState == Entry.MenuState.BSides && currentEntry.BSides is not null ||
					 currentEntry.CurrentMenuState == Entry.MenuState.CSides && currentEntry.CSides is not null) 
				{
					List<Menu>? menuList = null;
					switch (currentEntry.CurrentMenuState)
					{
						case Entry.MenuState.ASides:
							menuList = currentEntry.ASides;
							break;
						case Entry.MenuState.BSides:
							menuList = currentEntry.BSides;
							break;
						case Entry.MenuState.CSides:
							menuList = currentEntry.CSides;
							break;
					}
					if (menuList is null) goto End;
					int menuIndex () => currentEntry.CurrentMenuState switch
					{
						Entry.MenuState.BSides => currentEntry.currentBSideMenuIndex,
						Entry.MenuState.CSides => currentEntry.currentCSideMenuIndex,
						_ => currentEntry.currentASideMenuIndex
					};

                    void setMenuIndex (int value) {
						switch (currentEntry.CurrentMenuState)
						{
							case Entry.MenuState.BSides: currentEntry.currentBSideMenuIndex = value; break;
							case Entry.MenuState.CSides: currentEntry.currentCSideMenuIndex = value; break;
							default: currentEntry.currentASideMenuIndex = value; break;
						}
					}
					
					if (menuIndex() < 1) goto End;
					
					menuList[menuIndex()].Focused = false;
					var ind = menuList[menuIndex()].Index;
					setMenuIndex(menuIndex() - 1);
					Audio.Play(Sfx.main_menu_roll_down);
					
					menuList[menuIndex()].Focused = true;
					menuList[menuIndex()].Index = ind;

					End: {}
				}
			}
		}
		else if (state == States.Restarting)
		{
			restartConfirmMenu.Update();

			if (Controls.Confirm.ConsumePress())
			{
				if (restartConfirmMenu.Index == 1)
				{
					Audio.Play(Sfx.main_menu_start_game);
					Game.Instance.Music.Stop();
					Save.Instance.EraseRecord(currentEntry.Level.ID);
					state = States.Entering;
				}
				else
				{
					Audio.Play(Sfx.main_menu_restart_cancel);
					state = States.Selected;
				}
			}
			else if (Controls.Cancel.ConsumePress())
			{
				Audio.Play(Sfx.main_menu_restart_cancel);
				state = States.Selected;
			}
		}
		else if (state == States.Entering)
		{
			if (cameraCloseUpEase >= 1.0f)
			{
				currentEntry.Level.Enter(new SlideWipe(), 1.5f);
			}
		} 
		else if (state == States.EnteringCheckpoint) 
		{
			if (cameraCloseUpEase >= 1.0f) {
				currentEntry.Level.Enter(currentEntry.SelectedCheckpoint ?? Map.StartCheckpoint, new SlideWipe(), 1.5f);
				Save.CurrentRecord.Checkpoint = currentEntry.SelectedCheckpoint ?? Map.StartCheckpoint;
			}
		}
		else if (state == States.EnteringSubmap)
		{
			if (cameraCloseUpEase >= 1.0f) {
				if (currentEntry.SelectedSubmap is null || !Assets.Maps.ContainsKey(currentEntry.SelectedSubmap)) goto End;
				Save.Instance.LevelID = currentEntry.Level.ID;

				World? world = null;

				Game.Instance.Goto(new Transition()
				{
					Mode = Transition.Modes.Replace,
					Scene = () => {
						world = new World(new(currentEntry.Level.Map, Save.CurrentRecord.Checkpoint, false, World.EntryReasons.Entered));
						return world;
					},
					ToBlack = new SlideWipe(),
					FromBlack = new SpotlightWipe(),
					StopMusic = true,
					HoldOnBlackFor = 1.5f,
					Callback = () => {
						if (world == null) return;
						Cassette? cassette = world.All<Cassette>().Find(
							actor => {
								if (actor is Cassette cassette && cassette.Map == currentEntry.SelectedSubmap) return true;
								return false;
							}
						) as Cassette;
						if (cassette != null && world.Get<Player>() is {} player)
						{
							world.Camera.Position = cassette.Position + new Vec3(20, 20, 40);
							player.EnterCassette(cassette);
						}
					}
				});
				
				End: {}
			}
		}
		else if (state == States.EntertingCSide)
		{
			if (cameraCloseUpEase >= 1.0f)
			{
				Save.Instance.LevelID = currentEntry.Level.ID;
				Save.CurrentRecord.CSideCheckpoint = currentEntry.SelectedCheckpoint ?? Map.StartCheckpoint;
				Game.Instance.Goto(new Transition()
				{
					Mode = Transition.Modes.Replace,
					Scene = () => new World(new(currentEntry.Level.CSideMap, currentEntry.SelectedCheckpoint ?? Map.StartCheckpoint, false, World.EntryReasons.Entered, true)),
					ToBlack = new SnowWipe(),
					StopMusic = true,
					HoldOnBlackFor = 1.5f,
					FromPause = false
				});
			}
		}
	}

	public override void Render(Target target)
	{
		target.Clear(0x0b090d, 1, 0, ClearMask.All);

		// update entry textures
		foreach (var entry in entries)
		{
			var flip = (entry.SelectionEase > 0.50f ? 1 : -1);
			var shine = MathF.Max(0, MathF.Max(-wobble.X, flip * wobble.Y)) * entry.HighlightEase;
			entry.Redraw(batch, shine);
			batch.Clear();
		}

		// draw each entry to the screen
		var camera = new Camera
		{
			Target = target,
			Position = new Vec3(0, -100 + 30 * Ease.Cube.In(cameraCloseUpEase), 0),
			LookAt = new Vec3(0, 0, 0),
			NearPlane = 1,
			FarPlane = 1000
		};

		for (int i = 0; i < entries.Count; i ++)
		{
			var it = entries[i];
			var shift = Ease.Cube.In(1.0f - it.HighlightEase) * 30 - Ease.Cube.In(it.SelectionEase) * 20;
			if (i != index)
				shift += Ease.Cube.InOut(selectedEase) * 50;
			var position = new Vec3((i - slide) * 60, shift, 0);
			var rotation = Ease.Cube.InOut(it.SelectionEase);
			var matrix = 
				Matrix.CreateScale(new Vec3(it.SelectionEase >= 0.50f ? -1 : 1, 1, 1)) *
				Matrix.CreateRotationX(wobble.Y * it.HighlightEase) *
				Matrix.CreateRotationZ(wobble.X * it.HighlightEase) *
				Matrix.CreateRotationZ((IsEntering ? -1 : 1) * rotation * MathF.PI) *
				Matrix.CreateTranslation(position);

            if (material.Shader?.Has("u_matrix") ?? false)
			    material.Set("u_matrix", matrix * camera.ViewProjection);
            if (material.Shader?.Has("u_near") ?? false)
			    material.Set("u_near", camera.NearPlane);
            if (material.Shader?.Has("u_far") ?? false)
			    material.Set("u_far", camera.FarPlane);
            if (material.Shader?.Has("u_texture") ?? false)
			    material.Set("u_texture", it.Target);
            if (material.Shader?.Has("u_texture_sampler") ?? false)
			    material.Set("u_texture_sampler", new TextureSampler(TextureFilter.Linear, TextureWrap.ClampToEdge, TextureWrap.ClampToEdge));

			var cmd = new DrawCommand(target, mesh, material)
			{
				DepthMask = true,
				DepthCompare = DepthCompare.Less,
				CullMode = CullMode.None
			};
			cmd.Submit();
		}

		// overlay
		{
			batch.SetSampler(new TextureSampler(TextureFilter.Linear, TextureWrap.ClampToEdge, TextureWrap.ClampToEdge));
			var bounds = new Rect(0, 0, target.Width, target.Height);
			var scroll = -new Vec2(1.25f, 0.9f) * (float)(Time.Duration.TotalSeconds) * 0.05f;

			// confirmation
			if (state == States.Restarting)
			{
				batch.Rect(bounds, Color.Black * 0.90f);
				restartConfirmMenu.Render(batch, bounds.Center);
			}

			batch.PushBlend(BlendMode.Add);
			batch.PushSampler(new TextureSampler(TextureFilter.Linear, TextureWrap.Repeat, TextureWrap.Repeat));
			batch.Image(Assets.Textures["overworld/overlay"], 
				bounds.TopLeft, bounds.TopRight, bounds.BottomRight, bounds.BottomLeft,
				scroll + new Vec2(0, 0), scroll + new Vec2(1, 0), scroll + new Vec2(1, 1), scroll + new Vec2(0, 1),
				Color.White * 0.10f);
			batch.PopBlend();
			batch.PopSampler();
			batch.Image(Assets.Textures["overworld/vignette"], 
				bounds.TopLeft, bounds.TopRight, bounds.BottomRight, bounds.BottomLeft,
				new Vec2(0, 0), new Vec2(1, 0), new Vec2(1, 1), new Vec2(0, 1),
				Color.White * 0.30f);

			// button prompts
			if (!IsEntering)
			{
				var cancelPrompt = Loc.Str(state == States.Selecting ? "back" : "cancel");
				var at = bounds.BottomRight + new Vec2(-16, -4) * Game.RelativeScale + new Vec2(0, -UI.PromptSize);
				var width = 0.0f;
				UI.Prompt(batch, Controls.Cancel, cancelPrompt, at, out width, 1.0f);
				at.X -= width + 8 * Game.RelativeScale;
				UI.Prompt(batch, Controls.Confirm, Loc.Str("confirm"), at, out _, 1.0f);

				// show version number on Overworld as well
                UI.Text(batch, Game.VersionString, bounds.BottomLeft + new Vec2(4, -4) * Game.RelativeScale, new Vec2(0, 1), Color.White * 0.25f);
            }

			if (cameraCloseUpEase > 0)
			{
				batch.PushBlend(BlendMode.Subtract);
				batch.Rect(bounds, Color.White * Ease.Cube.In(cameraCloseUpEase));
				batch.PopBlend();
			}
			batch.Render(target);
			batch.Clear();
		}
	}
}