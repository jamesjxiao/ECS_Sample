using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Rendering;

namespace ECS_FindTarget.Normal
{
    public struct Unit : IComponentData
    {

    }

    public struct Target : IComponentData
    {

    }

    public struct HasTarget : IComponentData
    {
        public Entity target;
    }

    public struct MoveSpeed : IComponentData
    {
        public float Value;
    }

    [DisableAutoCreation]
    public class FindTargetSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<Unit>().WithNone<HasTarget>().ForEach((Entity entity, ref Translation translation) =>
            {
                float3 curPos = translation.Value;
                float minDistance = float.MaxValue;
                Entity selectedEntity = Entity.Null;

                Entities.WithAll<Target>().ForEach((Entity targetEntity, ref Translation targetTranslation) =>
                {
                    float targetDistance = math.distance(curPos, targetTranslation.Value);
                    if (targetDistance < minDistance)
                    {
                        minDistance = targetDistance;
                        selectedEntity = targetEntity;
                    }
                });

                if (selectedEntity != Entity.Null)
                {
                    // 添加HasTarget组件数据
                    PostUpdateCommands.AddComponent(entity, new HasTarget() { target = selectedEntity });
                }

            });
        }
    }

    // 向目标点移动
    [DisableAutoCreation]
    public class UnitMoveSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref Translation translation, ref HasTarget hasTarget, ref MoveSpeed moveSpeed) =>
            {
            // 首先要判断entity是否还存在
            if (World.DefaultGameObjectInjectionWorld.EntityManager.Exists(hasTarget.target))
                {
                // 获取目标Entity的数据
                Translation targetTranslation = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Translation>(hasTarget.target);

                // 向目标点移动
                float3 moveDir = targetTranslation.Value - translation.Value;
                    float distance = math.length(moveDir);
                    moveDir = math.normalize(moveDir);

                    float moveDelta = moveSpeed.Value * UnityEngine.Time.deltaTime;
                // 防止跑过
                moveDelta = math.min(distance, moveDelta);
                    translation.Value = translation.Value + moveDir * moveDelta;

                // 距离到达
                if (math.distance(translation.Value, targetTranslation.Value) < 0.1f)
                    {
                    // 移除HasTarget组件
                    PostUpdateCommands.RemoveComponent(entity, typeof(HasTarget));
                    // 销毁targetEntity
                    PostUpdateCommands.DestroyEntity(hasTarget.target);
                    }
                }
                else
                {
                // 移除HasTarget组件
                PostUpdateCommands.RemoveComponent(entity, typeof(HasTarget));
                }

            });
        }
    }

    // 辅助线
    [DisableAutoCreation]
    public class HasTargetDebug : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref HasTarget hasTarget, ref Translation translation) =>
            {
                if (World.DefaultGameObjectInjectionWorld.EntityManager.Exists(hasTarget.target))
                {
                    Translation targetTranslation = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Translation>(hasTarget.target);
                    Debug.DrawLine(translation.Value, targetTranslation.Value, Color.red);
                }
                else
                {
                    PostUpdateCommands.RemoveComponent<HasTarget>(entity);
                }
            });
        }
    }

    public class ECS_FindTarget_ForEach : MonoBehaviour
    {
        public Material redMaterial;
        public Material blueMaterial;
        public Mesh mesh;
        public float areaWidth = 50f;

        private EntityManager entityManager = null;
        private float spawnUnit = 1f;
        private float spawnTarget = 1f;
        private int leftUnitNum = 10;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            for (int i = 0; i < 2; ++i)
            {
                SpawnUnitEntity();
            }

            for (int i = 0; i < 10; ++i)
            {
                SpawnTargetEntity();
            }
        }

        private void Update()
        {
            spawnTarget -= UnityEngine.Time.deltaTime;

            if (spawnTarget < 0f)
            {
                SpawnTargetEntity();
                spawnTarget = 0.1f;
            }

            if (leftUnitNum > 0)
            {
                spawnUnit -= UnityEngine.Time.deltaTime;
                if (spawnUnit < 0f)
                {
                    SpawnUnitEntity();
                    spawnUnit = 1f;
                    leftUnitNum--;
                }
            }
        }

        private void SpawnUnitEntity()
        {
            var entity = entityManager.CreateEntity(
                typeof(LocalToWorld),
                typeof(Translation),
                typeof(Rotation),
                typeof(NonUniformScale),
                typeof(RenderMesh),
                typeof(MoveSpeed),
                typeof(Unit));

            entityManager.SetComponentData(entity, new Translation()
            {
                Value = new float3(UnityEngine.Random.Range(-areaWidth, areaWidth), 0f, UnityEngine.Random.Range(-areaWidth, areaWidth))
            });

            entityManager.SetComponentData(entity, new NonUniformScale()
            {
                Value = new float3(1f, 1f, 1f) * 2f
            });

            entityManager.SetComponentData(entity, new MoveSpeed()
            {
                Value = UnityEngine.Random.Range(10f, 100f)
            });

            entityManager.SetSharedComponentData(entity, new RenderMesh()
            {
                mesh = mesh,
                material = redMaterial,
            });
        }

        private void SpawnTargetEntity()
        {
            var entity = entityManager.CreateEntity(
                typeof(LocalToWorld),
                typeof(Translation),
                typeof(Rotation),
                typeof(NonUniformScale),
                typeof(RenderMesh),
                typeof(Target));

            entityManager.SetComponentData(entity, new Translation()
            {
                Value = new float3(UnityEngine.Random.Range(-areaWidth, areaWidth), 0f, UnityEngine.Random.Range(-areaWidth, areaWidth))
            });

            entityManager.SetComponentData(entity, new NonUniformScale()
            {
                Value = new float3(1f, 1f, 1f)
            });

            entityManager.SetSharedComponentData(entity, new RenderMesh()
            {
                mesh = mesh,
                material = blueMaterial,
            });
        }
    }
}
