
namespace Celeste64;

public class SemiSolidPlatform : Solid
{
    private const float TransparancyEaseTime = 0.3f;
    private const float DisabledAlpha = 0.7f;
    private float alpha = 1;

    public bool IsOn = true;

    public SemiSolidPlatform()
	{
		SetOn(true);
	}

    public override void Added()
    {
		Model.MakeMaterialsUnique();
        base.Added();
    }

    public void SetOn(bool enabled)
	{
        if (IsOn == enabled) return;
        IsOn = enabled;
	}

    // Checks if the player is under the platform, used to determine if the platform is solid
    public bool CheckForPlayerUnder(Player player)
    {
        if (player.Position.Z >= WorldBounds.Max.Z)
            return false;

        var bounds = WorldBounds.Inflate(150);
        return bounds.Contains(player.Position with { Z = bounds.Center.Z });
    }

    private void UpdateModel() {
        foreach (var mat in Model.Materials)
			mat.Color = Color.White * alpha;
        
        if (IsOn && alpha == 1)
        {
            Model.Flags = ModelFlags.Terrain;
            Collidable = true;
        }
        else
        {
            Model.Flags = ModelFlags.Transparent;
            Collidable = false;
        }
    }

    public override void Update()
    {
        // Updating state based on player
        if (World.Get<Player>() is {} player)
            SetOn(!CheckForPlayerUnder(player));

        if (IsOn && alpha < 1)
        {
            Calc.Approach(ref alpha, 1, World.DeltaTime / TransparancyEaseTime);
            UpdateModel();
        }
        else if (!IsOn && alpha > DisabledAlpha) {
            Calc.Approach(ref alpha, DisabledAlpha, World.DeltaTime / TransparancyEaseTime);
            UpdateModel();
        }

        base.Update();
    }
}