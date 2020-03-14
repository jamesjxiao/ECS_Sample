using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Burst;
using Unity.Collections;

namespace ECS_FindTarget.Job
{
    // 每个Unit.FindTarget使用一个Job，多个Unit可以并行
    [DisableAutoCreation]
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
        private struct FindTargetJob : IJobForEachWithEntity<Translation>
        {
            // job完成自动释放
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<EntityWithPosition> targets;         
            // 操作缓存
            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation)
            {
                float3 curPos = translation.Value;
                float minDistance = float.MaxValue;
                Entity selectedEntity = Entity.Null;

                for(int i = 0; i < targets.Length; ++i)
                {
                    float targetDistance = math.distance(curPos, targets[i].position);
                    if (targetDistance < minDistance)
                    {
                        minDistance = targetDistance;
                        selectedEntity = targets[i].entity;
                    }
                }

                if (selectedEntity != Entity.Null)
                {
                    // 添加HasTarget组件数据，需要指明索引
                    ecb.AddComponent(index, entity, new HasTarget() { target = selectedEntity });
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
            // 查询目标entities
            EntityQuery targetQuery = GetEntityQuery(typeof(Target), ComponentType.ReadOnly<Translation>());
            NativeArray<Entity> targetEntityArray = targetQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<Translation> targetTranslationArray = targetQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

            // 构造job参数
            NativeArray<EntityWithPosition> args = new NativeArray<EntityWithPosition>(targetEntityArray.Length, Allocator.TempJob);
            for(int i = 0; i < args.Length; ++i)
            {
                args[i] = new EntityWithPosition()
                {
                    entity = targetEntityArray[i],
                    position = targetTranslationArray[i].Value,
                };
            }

            // 释放临时NativeArray
            targetEntityArray.Dispose();
            targetTranslationArray.Dispose();

            // 生成一个Job，然后调用
            FindTargetJob job = new FindTargetJob()
            {
                targets = args,
                ecb = endSimulationECBS.CreateCommandBuffer().ToConcurrent(),
            };
            
            JobHandle handle = job.Schedule(this, inputDeps);
            endSimulationECBS.AddJobHandleForProducer(handle);
            return handle;
        }
    }

}
