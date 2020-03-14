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
    // 每个Unit.FindTarget使用一个Job，多个Unit可以并行
    public class FindTargetSystem : JobComponentSystem
    {
        #region job define
        private struct EntityWithPosition
        {
            public Entity entity;
            public float3 position;
        }

        [RequireComponentTag(typeof(Unit))]
        [ExcludeComponent(typeof(HasTarget))]
        [BurstCompile]
        private struct FindTargetWithQuardrantJob : IJobForEachWithEntity<Translation, QuardrantType>
        {
            [ReadOnly]
            public NativeMultiHashMap<int, QuardrantData> multiHasMap;
            // 操作缓存
            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation, [ReadOnly]ref QuardrantType quardrantType)
            {
                float3 curPos = translation.Value;
                float minDistance = float.MaxValue;
                Entity selectedEntity = Entity.Null;
                int hasKey = QuardrantSystem.GetPositionHasMapKey(curPos);

                // 查找周围的9个区块
                FindTarget(hasKey, curPos, quardrantType, ref selectedEntity, ref minDistance);
                FindTarget(hasKey - 1, curPos, quardrantType, ref selectedEntity, ref minDistance);
                FindTarget(hasKey + 1, curPos, quardrantType, ref selectedEntity, ref minDistance);
                FindTarget(hasKey + QuardrantSystem.quardrantYMultiplier - 1, curPos, quardrantType, ref selectedEntity, ref minDistance);
                FindTarget(hasKey + QuardrantSystem.quardrantYMultiplier, curPos, quardrantType, ref selectedEntity, ref minDistance);
                FindTarget(hasKey + QuardrantSystem.quardrantYMultiplier - 1, curPos, quardrantType, ref selectedEntity, ref minDistance);
                FindTarget(hasKey - QuardrantSystem.quardrantYMultiplier - 1, curPos, quardrantType, ref selectedEntity, ref minDistance);
                FindTarget(hasKey - QuardrantSystem.quardrantYMultiplier, curPos, quardrantType, ref selectedEntity, ref minDistance);
                FindTarget(hasKey - QuardrantSystem.quardrantYMultiplier - 1, curPos, quardrantType, ref selectedEntity, ref minDistance);

                if (selectedEntity != Entity.Null)
                {
                    // 添加HasTarget组件数据，需要指明索引
                    ecb.AddComponent(index, entity, new HasTarget() { target = selectedEntity });
                }
            }

            private void FindTarget(int hasKey, float3 unitPosition, QuardrantType quardrantType, ref Entity selectedEntity, ref float selectedDistance)
            {
                QuardrantData data;
                NativeMultiHashMapIterator<int> iterator;

                if(multiHasMap.TryGetFirstValue(hasKey, out data, out iterator))
                {
                    do
                    {
                        if(quardrantType.type != data.quardrantEntity.type)
                        {
                            if(selectedEntity == Entity.Null)
                            {
                                selectedEntity = data.entity;
                                selectedDistance = math.distance(unitPosition, data.position);
                            }
                            else
                            {
                                float curDistance = math.distance(unitPosition, data.position);
                                if(curDistance < selectedDistance)
                                {
                                    selectedEntity = data.entity;
                                    selectedDistance = math.distance(unitPosition, data.position);
                                }
                            }
                        }
                    }
                    while(multiHasMap.TryGetNextValue(out data, ref iterator));
                }
            }
        }
        #endregion

        private EndSimulationEntityCommandBufferSystem endSimulationECBS;

        protected override void OnCreate()
        {
            endSimulationECBS = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            base.OnCreate();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // 生成一个Job，然后调用
            FindTargetWithQuardrantJob job = new FindTargetWithQuardrantJob()
            {
                multiHasMap = QuardrantSystem.quardrantMultiHasMap,
                ecb = endSimulationECBS.CreateCommandBuffer().ToConcurrent(),
            };

            JobHandle handle = job.Schedule(this, inputDeps);
            endSimulationECBS.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}
