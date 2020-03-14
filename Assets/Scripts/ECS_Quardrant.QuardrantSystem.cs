using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

namespace ECS_Quardrant
{
    //[DisableAutoCreation]
    public class QuardrantSystem : ComponentSystem
    {
        public const int quardrantYMultiplier = 1000;
        public const int quardrantCellSize = 20;

        public static int GetPositionHasMapKey(float3 position)
        {
            return (int)(math.floor(position.x / quardrantCellSize) + quardrantYMultiplier * math.floor(position.z / quardrantCellSize));
        }

        public static void DebugDrawQuadrant(float3 position)
        {
            Vector3 lowerLeft = new Vector3(math.floor(position.x / quardrantCellSize) * quardrantCellSize,
                math.floor(position.z / quardrantCellSize) * quardrantCellSize);

            Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(1, 0, 0) * quardrantCellSize);
            Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(0, 0, 1) * quardrantCellSize);
            Debug.DrawLine(lowerLeft + new Vector3(1, 0, 0) * quardrantCellSize, lowerLeft + new Vector3(1, 0, 1) * quardrantCellSize);
            Debug.DrawLine(lowerLeft + new Vector3(0, 0, 1) * quardrantCellSize, lowerLeft + new Vector3(1, 0, 1) * quardrantCellSize);
        }

        // 统计一个key中有多少个entity
        private static int GetEntityCountInHasMap(NativeMultiHashMap<int, Entity> map, int key)
        {
            Entity entity;
            NativeMultiHashMapIterator<int> nativeIterator;
            int count = 0;
            if(map.TryGetFirstValue(key, out entity, out nativeIterator))
            {
                do
                {
                    count++;
                }
                while(map.TryGetNextValue(out entity, ref nativeIterator));
            }
            return count;
        }

        [BurstCompile]
        private struct SetQuadrantDataHasMapJob : IJobForEachWithEntity<Translation, QuardrantType>
        {
            public NativeMultiHashMap<int, QuardrantData>.ParallelWriter map;

            public void Execute(Entity entity, int index, ref Translation translation, ref QuardrantType quardrantEntity)
            {
                int key = GetPositionHasMapKey(translation.Value);
                map.Add(key, new QuardrantData()
                {
                    entity = entity,
                    position = translation.Value,
                    quardrantEntity = quardrantEntity,
                });
            }
        }

        public static NativeMultiHashMap<int, QuardrantData> quardrantMultiHasMap;

        protected override void OnCreate()
        {
            quardrantMultiHasMap = new NativeMultiHashMap<int, QuardrantData>(20000, Allocator.Persistent);
            base.OnCreate();
        }

        protected override void OnDestroy()
        {
            quardrantMultiHasMap.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            // 计算每个对象的hash，然后放到对应的map中
            EntityQuery query = GetEntityQuery(typeof(Translation), typeof(QuardrantType));

            /* 放到job中执行
            Entities.ForEach((Entity entity, ref Translation translation) =>
            {
                int hasMapKey = GetPositionHasMapKey(translation.Value);
                quardrantMultiHasMap.Add(hasMapKey, entity);
            });
            */

            quardrantMultiHasMap.Clear();
            if (quardrantMultiHasMap.Length < query.CalculateEntityCount())
            {
                // quardrantMultiHasMap.Capacity = query.CalculateEntityCount();
            }
            //quardrantMultiHasMap = new NativeMultiHashMap<int, QuardrantData>(query.CalculateEntityCount(), Allocator.TempJob);

            SetQuadrantDataHasMapJob job = new SetQuadrantDataHasMapJob()
            {
                map = quardrantMultiHasMap.AsParallelWriter(),
            };
            var handle = JobForEachExtensions.Schedule(job, query);
            handle.Complete();

            // DebugDrawQuadrant(UtilClass.GetMouseWorldPosition(Camera.main));
            // 光标所在象限entity的个数
            // Debug.Log(GetEntityCountInHasMap(quardrantMultiHasMap, GetPositionHasMapKey(UtilClass.GetMouseWorldPosition())));
        }
    }
}