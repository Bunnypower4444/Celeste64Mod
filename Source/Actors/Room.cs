
using Sledge.Formats;

namespace Celeste64;

public class Room(string targetRoomName, bool isSecret = false) : Actor
{
    public readonly string TargetRoomName = targetRoomName;
    public readonly bool Secret = isSecret;

    private bool activated = false;
    private float wallAlpha = 1;
    private List<RoomWall> walls = [];

    public override void Added()
    {
        base.Added();
        if (string.IsNullOrEmpty(TargetRoomName))
            return;
        walls = World.All<RoomWall>().Cast<RoomWall>().Where(wall => wall.RoomName == TargetRoomName).ToList();
        UpdateWalls();
    }

    public override void Update()
    {
        base.Update();

        // Fade out if the room contains the player
        if (World.Get<Player>() is {} player && WorldBounds.Contains(player.Position))
        {
            if (!activated && Secret)
            {
                Audio.Play(Sfx.sfx_secret, Position);
                activated = true;
            }
            
            if (wallAlpha > 0)
            {
                Calc.Approach(ref wallAlpha, 0, World.DeltaTime * 2);
                UpdateWalls();
            }
        }
        else if (wallAlpha < 1)
        {
            Calc.Approach(ref wallAlpha, 1, World.DeltaTime * 2);
            UpdateWalls();
        }
    }

    private void UpdateWalls()
    {
        foreach (var wall in walls)
            wall.UpdateWall(wallAlpha, WorldBounds.Center);
    }
}

public class RoomWall(string roomName) : Solid
{
    // Basically a SimpleModel, but has a custom Prepare method
    //  so we can cull the faces we don't want
    private class RoomWallModel(SimpleModel simpleModel) : Model
    {
        public readonly SimpleModel SimpleModel = simpleModel;
        private Vec3 roomCenter;
        public Vec3 RoomCenter { get => roomCenter; set {
            if (roomCenter != value)
            {
                roomCenter = value;
                dirty = true;
            }
        } }
        public float Alpha = 1;
        private Matrix actorMatrix;
        public Matrix ActorMatrix { get => actorMatrix; set {
            if (actorMatrix != value)
            {
                actorMatrix = value;
                dirty = true;
            }
        } }

        private bool dirty;
        // Where the section of triangles facing away from the center
        //  starts for each Part
        private readonly List<int> backFacingPartIndexStarts = [];

        public override void Prepare()
        {
            base.Prepare();

            if (dirty)
            {                
                backFacingPartIndexStarts.Clear();
                var vertices = SimpleModel.Vertices;
                var indices = SimpleModel.Indices;
                // vertices are local, so the center needs to be transformed
                Vec3 localRoomCenter = Vec3.Zero;
                if (Matrix.Invert(actorMatrix, out var inv))
                    localRoomCenter = Vec3.Transform(roomCenter, inv);

                foreach (var segment in SimpleModel.Parts)
                {
                    // the border between front- and back-facing triangles
                    int frontFacingIndex = segment.IndexStart;
                    // Reorder so that triangles facing the front are at the beginning
                    //  (thanks to Sebastian Lague https://www.youtube.com/watch?v=C1H4zIiCOaI&t=693s)
                    for (int i = segment.IndexStart; i < segment.IndexStart + segment.IndexCount; i += 3)
                    {
                        // Check if it is facing the center (all vertices of a triangle should have the same normal)
                        if (vertices[indices[i]].Normal.Dot(localRoomCenter - vertices[indices[i]].Pos) >= 0)
                        {
                            // Swap if it isn't at the beginning part of the list
                            if (frontFacingIndex != i - segment.IndexStart)
                            {
                                (indices[i], indices[frontFacingIndex]) = (indices[frontFacingIndex], indices[i]);
                                (indices[i + 1], indices[frontFacingIndex + 1]) = (indices[frontFacingIndex + 1], indices[i + 1]);
                                (indices[i + 2], indices[frontFacingIndex + 2]) = (indices[frontFacingIndex + 2], indices[i + 2]);
                            }
                            frontFacingIndex += 3;
                        }
                    }
                    backFacingPartIndexStarts.Add(frontFacingIndex);
                }

                SimpleModel.Mesh.SetIndices<int>(indices);
            }
        }

        // Basically the same as SimpleModel.Render, but with some extra steps
        //  because transparent faces should not have DepthMask, but others should
        public override void Render(ref RenderState state)
        {
            /*
            // Make sure DepthMask is on, because the ModelFlags.Transparent
            //  turns it off
            var prevDepthMask = state.DepthMask;
            state.DepthMask = true;
            SimpleModel.Render(ref state);
            state.DepthMask = prevDepthMask; */
            
            // Just use SimpleModel's Render() if the effect isn't on
            if (Alpha == 1)
                SimpleModel.Render(ref state);
            else
            {
                foreach (var mat in SimpleModel.Materials)
                {
                    state.ApplyToMaterial(mat, Matrix.Identity);

                    if (mat.Shader != null &&
                        mat.Shader.Has("u_jointMult"))
                        mat.Set("u_jointMult", 0.0f);
                    
                    if (mat.Shader?.Has("u_roomWallTransparency") ?? false)
                        mat.Set("u_roomWallTransparency", 1 - Alpha);
                    if (mat.Shader?.Has("u_roomWallCenter") ?? false)
                        mat.Set("u_roomWallCenter", RoomCenter);
                }

                for (int i = 0; i < SimpleModel.Parts.Count; i++)
                {
                    var segment = SimpleModel.Parts[i];
                    if (segment.IndexCount <= 0 || segment.MaterialIndex < 0)
                        continue;

                    // Render the front-facing stuff (if there are any)
                    /* if (backFacingPartIndexStarts[i] > segment.IndexStart)
                    {
                        var call = new DrawCommand(state.Camera.Target, SimpleModel.Mesh, SimpleModel.Materials[segment.MaterialIndex])
                        {
                            DepthCompare = state.DepthCompare,
                            DepthMask = state.DepthMask,
                            CullMode = SimpleModel.CullMode,
                            MeshIndexStart = segment.IndexStart,
                            MeshIndexCount = backFacingPartIndexStarts[i] - segment.IndexStart
                        };
                        call.Submit();
                        state.Calls++;
                    } */

                    // Render the back-facing stuff without DepthMask (if there are any)
                    if (backFacingPartIndexStarts[i] < segment.IndexStart + segment.IndexCount)
                    {
                        var call = new DrawCommand(state.Camera.Target, SimpleModel.Mesh, SimpleModel.Materials[segment.MaterialIndex])
                        {
                            DepthCompare = state.DepthCompare,
                            DepthMask = false,
                            CullMode = SimpleModel.CullMode,
                            MeshIndexStart = backFacingPartIndexStarts[i],
                            MeshIndexCount = segment.IndexStart + segment.IndexCount - backFacingPartIndexStarts[i]
                        };
                        call.Submit();
                        state.Calls++;
                    }
                    
                    state.Triangles += segment.IndexCount / 3;
                }
            }
        }
    }

    public readonly string RoomName = roomName;
    private RoomWallModel? roomWallModel;

    public override void Created()
    {
        base.Created();
        // the position will be updated later
        roomWallModel = new(Model) {
            ActorMatrix = Matrix
        };
    }

    public override void Added()
    {
        base.Added();
        Model.MakeMaterialsUnique();
    }

    protected override void Transformed()
    {
        base.Transformed();
        if (roomWallModel != null)
            roomWallModel.ActorMatrix = Matrix;
    }

    public override void CollectModels(List<(Actor Actor, Model Model)> populate)
    {
        if (roomWallModel != null)
            populate.Add((this, roomWallModel));
    }

    public void UpdateWall(float alpha, Vec3 roomCenter)
    {
        // foreach (var mat in Model.Materials)
        //     mat.Color = Color.White * alpha;
        if (roomWallModel == null)
            return;
        
        roomWallModel.Alpha = alpha;
        roomWallModel.RoomCenter = roomCenter;

        if (alpha == 1)
            roomWallModel.Flags = ModelFlags.Terrain;
        else
            roomWallModel.Flags = ModelFlags.RoomWall;

        // Visible = alpha > 0;
        // Transparent = alpha <= 0;
    }
}