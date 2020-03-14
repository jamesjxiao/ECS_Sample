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
    public class ECS_SpriteSheet : MonoBehaviour
    {
        public Mesh mesh;
        public Material materal;
        public Vector2 spwanArea = new Vector2(10f, 5f);
        public int spawnNumber = 1000;

        public Camera randerCamera = null;

        private static ECS_SpriteSheet instance = null;
        public static ECS_SpriteSheet Instance { get { return instance; } }

        private void Awake()
        {
            instance = this;
            
            randerCamera = Camera.main;

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityArchetype archetype = entityManager.CreateArchetype(typeof(Translation), typeof(SpriteSheetAnimationData));

            NativeArray<Entity> entities = new NativeArray<Entity>(spawnNumber, Allocator.Temp);

            entityManager.CreateEntity(archetype, entities);
            for (var i = 0; i < entities.Length; ++i)
            {
                var entity = entities[i];
                entityManager.SetComponentData(entity, new Translation()
                {
                    Value = new float3(UnityEngine.Random.Range(-spwanArea.x, spwanArea.x), UnityEngine.Random.Range(-spwanArea.y, spwanArea.x), 0f),
                });
                entityManager.SetComponentData(entity, new SpriteSheetAnimationData()
                {
                    frame = UnityEngine.Random.Range(0, 7),
                    frameCountX = 4,
                    frameCountY = 2,
                    frameCount = 8,
                    timer = UnityEngine.Random.Range(0f, 0.1f),
                    timerMax = 0.1f,
                });
            }
        }

    }
}