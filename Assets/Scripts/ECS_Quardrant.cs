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
    // 辅助线
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

    public class ECS_Quardrant : MonoBehaviour
    {
        public Material redMaterial;
        public Material blueMaterial;
        public Mesh mesh;
        public float areaWidth = 50f;

        public int initRedNum = 1000;
        public int redAddPerInterval = 10;
        public float redAddTimeDelta = 0.1f;

        public int initBlueNum = 10;
        public int blueAddPerInterval = 1;
        public float blueAddTimeDelta = 1f;

        private EntityManager entityManager = null;
        private float spawnUnit = 1f;
        private float spawnTarget = 1f;
        private int leftUnitNum = 10;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            for (int i = 0; i < initBlueNum; ++i)
            {
                SpawnUnitEntity();
            }

            for (int i = 0; i < initRedNum; ++i)
            {
                SpawnTargetEntity();
            }
        }

        private void Update()
        {
            spawnTarget -= UnityEngine.Time.deltaTime;

            if (spawnTarget < 0f)
            {
                for(int i = 0; i < redAddPerInterval; ++i)
                {
                    SpawnTargetEntity();
                }
                spawnTarget = redAddTimeDelta;
            }

            if (leftUnitNum > 0)
            {
                spawnUnit -= UnityEngine.Time.deltaTime;
                if (spawnUnit < 0f)
                {
                    for (int i = 0; i < blueAddPerInterval; ++i)
                    {
                        SpawnUnitEntity();
                    }
                    spawnUnit = blueAddTimeDelta;
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
                typeof(Unit),
                typeof(QuardrantType));

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

            entityManager.SetComponentData(entity, new QuardrantType()
            {
                type = QuardrantType.TypeEnum.Unit,
            });

            entityManager.SetSharedComponentData(entity, new RenderMesh()
            {
                mesh = mesh,
                material = blueMaterial,
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
                typeof(Target),
                typeof(QuardrantType));

            entityManager.SetComponentData(entity, new Translation()
            {
                Value = new float3(UnityEngine.Random.Range(-areaWidth, areaWidth), 0f, UnityEngine.Random.Range(-areaWidth, areaWidth))
            });

            entityManager.SetComponentData(entity, new NonUniformScale()
            {
                Value = new float3(1f, 1f, 1f)
            });

            entityManager.SetComponentData(entity, new QuardrantType()
            {
                type = QuardrantType.TypeEnum.Target,
            });

            entityManager.SetSharedComponentData(entity, new RenderMesh()
            {
                mesh = mesh,
                material = redMaterial,
            });
        }
    }
}
