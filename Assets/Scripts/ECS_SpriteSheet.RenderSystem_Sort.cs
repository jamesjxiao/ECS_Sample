using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ECS_SpriteSheet
{
    /// <summary>
    /// 带sort和cullin能力 job
    /// </summary>
    [UpdateAfter(typeof(SpriteSheetAnimationSystem))]
    [DisableAutoCreation]
    public partial class SpriteSheetRenderer_Sort : ComponentSystem
    {
        // 裁减和分组，没有排序
        private struct CullAndSortJob : IJobForEachWithEntity<SpriteSheetAnimationData, Translation>
        {
            public float yTop_1;
            public float yTop_2;
            public float yBottom;

            public NativeQueue<RenderData>.ParallelWriter nativeQueue_1;
            public NativeQueue<RenderData>.ParallelWriter nativeQueue_2;

            public void Execute(Entity entity, int index, ref SpriteSheetAnimationData animData, ref Translation translation)
            {
                float posY = translation.Value.y;

                // 在视野内，需要渲染
                if(posY > yBottom && posY < yTop_1)
                {
                    RenderData renderData = new RenderData()
                    {
                        entity = entity,
                        position = translation.Value,
                        matrix = animData.matrix,
                        uv = animData.uv,
                    };

                    // 分为上下两部分
                    if(posY > yTop_2)
                    {
                        nativeQueue_2.Enqueue(renderData);
                    }
                    else
                    {
                        nativeQueue_1.Enqueue(renderData);
                    }
                }
            }
        }

        // Queue -> Array
        [BurstCompile]
        private struct NativeQueueToArrayJob : IJob
        {
            public NativeQueue<RenderData> queue;
            public NativeArray<RenderData> array;
            public void Execute()
            {
                int index = 0;
                RenderData renderData;
                while (queue.TryDequeue(out renderData))
                {
                    array[index++] = renderData;
                }
            }
        }

        // 排序
        [BurstCompile]
        private struct SortByPositionJob : IJob
        {
            public NativeArray<RenderData> array;

            public void Execute()
            {
                for(int i = 0; i < array.Length; ++i)
                {
                    for(int j = i + 1; j < array.Length; ++j)
                    {
                        if(array[i].position.y < array[j].position.y)
                        {
                            var tmp = array[i];
                            array[i] = array[j];
                            array[j] = tmp;
                        }
                    }
                }
            }
        }

        // 数组合并
        [BurstCompile]
        private struct FillArrayParallelJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<RenderData> array;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<Matrix4x4> matrixs;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<Vector4> uvs;

            public int startingIndex;

            public void Execute(int index)
            {
                var renderData = array[index];
                matrixs[startingIndex + index] = renderData.matrix;
                uvs[startingIndex + index] = renderData.uv;
            }
        }
    }

    /// <summary>
    /// 带sort和cullin能力 main
    /// </summary>
    [UpdateAfter(typeof(SpriteSheetAnimationSystem))]
    public partial class SpriteSheetRenderer_Sort : ComponentSystem
    {
        private struct RenderData
        {
            public Entity entity;
            public float3 position;
            public Matrix4x4 matrix;
            public float4 uv;
        }

        private int ID_MainTex_UV;
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        Matrix4x4[] matrixs = new Matrix4x4[1023];
        Vector4[] uvs = new Vector4[1023];

        protected override void OnCreate()
        {
            base.OnCreate();
            ID_MainTex_UV = Shader.PropertyToID("_MainTex_UV");
        }

        protected override void OnUpdate()
        {
            NativeQueue<RenderData> queue1 = new NativeQueue<RenderData>(Allocator.TempJob);
            NativeQueue<RenderData> queue2 = new NativeQueue<RenderData>(Allocator.TempJob);

            var camera = ECS_SpriteSheet.Instance.randerCamera;
            float3 cameraPosition = camera.transform.position;
            float yBottom = cameraPosition.y - camera.orthographicSize;
            float yTop_1 = cameraPosition.y + camera.orthographicSize;
            float yTop_2 = cameraPosition.y;

            // 视野剔除和分组，分成2组
            CullAndSortJob cullJob = new CullAndSortJob()
            {
                yTop_1 = yTop_1,
                yTop_2 = yTop_2,
                yBottom = yBottom,
                nativeQueue_1 = queue1.AsParallelWriter(),
                nativeQueue_2 = queue2.AsParallelWriter(),
            };
            JobHandle cullHandle = cullJob.Schedule(this);
            cullHandle.Complete();

            NativeArray<RenderData> array1 = new NativeArray<RenderData>(queue1.Count, Allocator.TempJob);
            NativeArray<RenderData> array2 = new NativeArray<RenderData>(queue2.Count, Allocator.TempJob);
            NativeArray<JobHandle> arrayHandle = new NativeArray<JobHandle>(2, Allocator.TempJob);

            // Queue转换为Array
            NativeQueueToArrayJob convertJob1 = new NativeQueueToArrayJob()
            {
                queue = queue1,
                array = array1,
            };
            NativeQueueToArrayJob convertJob2 = new NativeQueueToArrayJob()
            {
                queue = queue2,
                array = array2,
            };
            arrayHandle[0] = convertJob1.Schedule();
            arrayHandle[1] = convertJob2.Schedule();
            JobHandle.CompleteAll(arrayHandle);

            queue1.Dispose();
            queue2.Dispose();

            // 排序
            SortByPositionJob sortJob1 = new SortByPositionJob()
            {
                array = array1
            };
            SortByPositionJob sortJob2 = new SortByPositionJob()
            {
                array = array2
            };
            arrayHandle[0] = sortJob1.Schedule();
            arrayHandle[1] = sortJob2.Schedule();
            JobHandle.CompleteAll(arrayHandle);

            // 可见对象合并，并提取渲染数据
            int visibleEntityTotal = array1.Length + array2.Length;
            NativeArray<Matrix4x4> globalMatrixs = new NativeArray<Matrix4x4>(visibleEntityTotal, Allocator.TempJob);
            NativeArray<Vector4> globalUVs = new NativeArray<Vector4>(visibleEntityTotal, Allocator.TempJob);
            FillArrayParallelJob combineJob1 = new FillArrayParallelJob()
            {
                array = array1,
                matrixs = globalMatrixs,
                uvs = globalUVs,
                startingIndex = 0,
            };
            arrayHandle[0] = combineJob1.Schedule(array1.Length, 10);
            FillArrayParallelJob combineJob2 = new FillArrayParallelJob()
            {
                array = array2,
                matrixs = globalMatrixs,
                uvs = globalUVs,
                startingIndex = array1.Length,
            };
            arrayHandle[1] = combineJob2.Schedule(array2.Length, 10);
            JobHandle.CompleteAll(arrayHandle);

            array1.Dispose();
            array2.Dispose();
            arrayHandle.Dispose();

            // 一次最多绘制1023个单位
            int totalNum = globalMatrixs.Length;
            int sliceNum = 1023;
            int groupNum = totalNum / sliceNum + (totalNum % sliceNum > 0 ? 1 : 0);
            for (int i = 0; i < groupNum; ++i)
            {
                int begin = i * sliceNum;
                int end = Mathf.Min((i + 1) * sliceNum, totalNum);

                NativeArray<Matrix4x4>.Copy(globalMatrixs, begin, matrixs, 0, end - begin);
                NativeArray<Vector4>.Copy(globalUVs, begin, uvs, 0, end - begin);

                mpb.SetVectorArray(ID_MainTex_UV, uvs);
                Graphics.DrawMeshInstanced(ECS_SpriteSheet.Instance.mesh, 0, ECS_SpriteSheet.Instance.materal, matrixs, end - begin, mpb);
            }

            globalMatrixs.Dispose();
            globalUVs.Dispose();
        }
    }
}