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
    /// <summary>
    /// 分成2个job实现，一个负责move[]，一个负责structural change
    /// 前者开启Burst，后者不开启
    /// </summary>
    //[DisableAutoCreation]
    public class UnitMoveSystem : JobComponentSystem
    {
        #region job define
        [RequireComponentTag(typeof(Unit))]
        [BurstCompile]
        private struct MoveToTargetJob : IJobForEachWithEntity<HasTarget, Translation, MoveSpeed>
        {
            // 操作缓存
            public EntityCommandBuffer.Concurrent ecb;

            [ReadOnly]
            public float deltaTime;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<float3> targetPositions;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<bool> targetExists;

            // 返回值
            public NativeArray<Entity> removeComponentEntities;

            public void Execute(Entity entity, int index, [ReadOnly]ref HasTarget hasTarget, ref Translation translation, [ReadOnly]ref MoveSpeed moveSpeed)
            {
                // 首先要判断entity是否还存在
                if (targetExists[index])
                {
                    // 获取目标Entity的数据
                    float3 targetPos = targetPositions[index];

                    // 向目标点移动
                    float3 moveDir = targetPos - translation.Value;
                    float distance = math.length(moveDir);
                    moveDir = math.normalize(moveDir);

                    float moveDelta = moveSpeed.Value * deltaTime;
                    // 防止跑过
                    moveDelta = math.min(distance, moveDelta);
                    translation.Value = translation.Value + moveDir * moveDelta;

                    // 距离到达
                    if (math.distance(translation.Value, targetPos) < 0.1f)
                    {
                        // burst not support
                        removeComponentEntities[index] = entity;
                        // burst support
                        ecb.DestroyEntity(index, hasTarget.target);
                    }
                }
                else
                {
                    removeComponentEntities[index] = entity;
                }
            }
        }

        [RequireComponentTag(typeof(Unit), typeof(HasTarget), typeof(MoveSpeed))]
        private struct RemoveComponentJob : IJobForEachWithEntity<Translation>
        {
            public EntityCommandBuffer.Concurrent ecb;
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> removeComponentEntities;

            public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation)
            {
                if (removeComponentEntities[index] != Entity.Null)
                    ecb.RemoveComponent(index, entity, typeof(HasTarget));
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
            // 查询
            EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<HasTarget>(), ComponentType.ReadOnly<Unit>(), ComponentType.ReadOnly<MoveSpeed>());
            NativeArray<HasTarget> hasTargets = query.ToComponentDataArray<HasTarget>(Allocator.TempJob);

            // 构造参数，预计算每个entity的target的位置
            NativeArray<float3> args_posiiton = new NativeArray<float3>(hasTargets.Length, Allocator.TempJob);
            NativeArray<bool> args_exist = new NativeArray<bool>(hasTargets.Length, Allocator.TempJob);

            // 返回参数
            NativeArray<Entity> entitiesNeedRemoveComponent = new NativeArray<Entity>(hasTargets.Length, Allocator.TempJob);

            for (int i = 0; i < args_posiiton.Length; ++i)
            {
                // Burst不支持
                if (World.DefaultGameObjectInjectionWorld.EntityManager.Exists(hasTargets[i].target))
                {
                    // Burst不支持
                    args_posiiton[i] = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Translation>(hasTargets[i].target).Value;
                    args_exist[i] = true;
                }
                else
                {
                    args_exist[i] = false;
                }
            }

            // 生成一个Job，然后调用
            MoveToTargetJob jobMove = new MoveToTargetJob()
            {
                ecb = endSimulationECBS.CreateCommandBuffer().ToConcurrent(),
                deltaTime = UnityEngine.Time.deltaTime,
                targetPositions = args_posiiton,
                targetExists = args_exist,
                removeComponentEntities = entitiesNeedRemoveComponent,
            };
            // 释放临时NativeArray
            hasTargets.Dispose();
            JobHandle handleMove = jobMove.Schedule(this, inputDeps);

            RemoveComponentJob jobClean = new RemoveComponentJob()
            {
                ecb = endSimulationECBS.CreateCommandBuffer().ToConcurrent(),
                removeComponentEntities = entitiesNeedRemoveComponent,
            };
            JobHandle handleClean = jobClean.Schedule(this, handleMove);

            endSimulationECBS.AddJobHandleForProducer(handleClean);
            return handleClean;
        }
    }
}