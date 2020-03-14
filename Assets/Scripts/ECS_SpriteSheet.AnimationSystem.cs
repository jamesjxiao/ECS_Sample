using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using Unity.Collections;

namespace ECS_SpriteSheet
{
    public struct SpriteSheetAnimationData : IComponentData
    {
        public int frame;
        public int frameCountX;
        public int frameCountY;
        public int frameCount;
        public float timer;
        public float timerMax;

        public float4 uv;
        public Matrix4x4 matrix;
    }

    [DisableAutoCreation]
    public class SpriteSheetAnimationSystem : JobComponentSystem
    {
        [BurstCompile]
        public struct AnimJob : IJobForEach<SpriteSheetAnimationData, Translation>
        {
            public float deltaTime;

            public void Execute(ref SpriteSheetAnimationData data, ref Translation translation)
            {
                data.timer += deltaTime;

                while (data.timer >= data.timerMax)
                {
                    data.timer -= data.timerMax;
                    data.frame = (data.frame + 1) % data.frameCount;
                }

                float uvWidth = 1f / data.frameCountX;
                float uvHeight = 1f / data.frameCountY;
                float uvOffsetX = uvWidth * (data.frame % data.frameCountX);
                float uvOffsetY = uvHeight * (data.frame / data.frameCountX);

                data.uv = new float4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);
                data.matrix = Matrix4x4.TRS(translation.Value, Quaternion.identity, Vector3.one);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            AnimJob job = new AnimJob()
            {
                deltaTime = UnityEngine.Time.deltaTime,
            };

            JobHandle handle = job.Schedule(this, inputDeps);
            return handle;
        }

    }
}