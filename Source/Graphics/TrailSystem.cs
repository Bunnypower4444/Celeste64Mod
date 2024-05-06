
namespace Celeste64;

public class TrailSystem
{
    private readonly List<Trail> trails = [];
    public float FadeTime = 0.5f;

    public void Update(float time)
    {
        foreach (var trail in trails)
        {
            if (trail.Percent < 1)
                trail.Percent += time / FadeTime;
        }
    }

    /// <summary>
    /// Creates a new trail. Old trails will be reused if they have faded out.
    /// </summary>
    /// <param name="lazyModels">A function that returns an array of the trail's models. Will only be called if a new trail is created or if forceUseNewModels is true.</param>
    /// <param name="transform"></param>
    /// <param name="color"></param>
    /// <param name="forceUseNewModels">Even if a trail is reused, new models from lazyModels will be used.</param>
    /// <returns></returns>
    public Trail CreateTrail(Func<Model[]> lazyModels, Matrix transform, Color color, bool forceUseNewModels = false)
    {
        Trail? trail = null;
		foreach (var it in trails)
			if (it.Percent >= 1)
			{
				trail = it;
				break;
			}
        
		if (trail == null)
			trails.Add(trail = new(lazyModels(), transform, color));
        else
        {
            if (forceUseNewModels)
                trail.Models = lazyModels();
            trail.Transform = transform;
            trail.Color = color;
        }

		trail.Percent = 0.0f;
        return trail;
    }

    /// <summary>
    /// Collects all the models for each trail and adds them to the populate
    /// </summary>
    /// <param name="populate">Where all the models are stored</param>
    /// <param name="parentMatrix">The matrix for the parent so that the trail models will move relative to the parent</param>
    public void CollectModels(List<Model> populate, Matrix parentMatrix)
    {
        foreach (var trail in trails)
        {
            if (Matrix.Invert(parentMatrix, out var inverse))
            {
                foreach (var model in trail.Models)
                    model.Transform = trail.Transform * inverse;
            }
            trail.CollectModels(populate);
        }
    }

    public class Trail
    {
        private Model[] models;
        public Model[] Models { get => models; set {
            models = value;
            foreach (var model in models)
            {
                model.Flags = ModelFlags.Transparent;
                model.MakeMaterialsUnique();
                foreach (var mat in model.Materials)
                {
                    mat.Texture = Assets.Textures["white"];
                    mat.Effects = 0;
                }
            }
        }}
        public Matrix Transform;
        public Color Color;
        public float Percent = 0;

        public Trail(Model[] models, Matrix transform, Color color)
        {
            // so the compiler will stop and textures are set up via Models.set
            Models = this.models = models;
            Transform = transform;
            Color = color;
        }

        public void CollectModels(List<Model> populate)
        {
            if (Percent >= 1)
				return;

			// I HATE this alpha fade out but don't have time to make some kind of full-model fade out effect
			var alpha = Ease.Cube.Out(Calc.ClampedMap(Percent, 0.5f, 1.0f, 1, 0));

            foreach (var model in Models)
            {
                foreach (var mat in model.Materials)
                    mat.Color = Color * alpha;

                if (model is Hair hair)
                    hair.Color = Color * alpha;
                
                populate.Add(model);
            }
        }
    }
}