using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Rendering;
using Unity.Mathematics;

public struct LevelComponent : IComponentData
{
    public float level;
}

public struct MoveSpeedComponent : IComponentData
{
    public float moveSpeed;
}

[DisableAutoCreation]
public class LevelUpSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref LevelComponent levelComponent) =>
        {
            levelComponent.level += 1f * Time.DeltaTime;
        });
    }
}

[DisableAutoCreation]
public class MoveSpeedSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref MoveSpeedComponent moveSpeedComp, ref Translation translation) =>
        {
            translation.Value.y += moveSpeedComp.moveSpeed * UnityEngine.Time.deltaTime;

            if (translation.Value.y > 20f)
                moveSpeedComp.moveSpeed = -Mathf.Abs(moveSpeedComp.moveSpeed);
            else if(translation.Value.y < -20f)
                moveSpeedComp.moveSpeed = Mathf.Abs(moveSpeedComp.moveSpeed);
        });
    }
}

public class ECS_First : MonoBehaviour
{
    public Mesh mesh;
    public Material material;

    void Start()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // 使用components类型创建entity
        Entity entity = entityManager.CreateEntity(typeof(LevelComponent));
        // 设置entity数据
        entityManager.SetComponentData(entity, new LevelComponent() { level = 10 });

        // 使用原型创建entity
        EntityArchetype entityArchetype = entityManager.CreateArchetype(typeof(LevelComponent), typeof(Translation));
        Entity entity2 = entityManager.CreateEntity(entityArchetype);

        // 批量创建entity
        NativeArray<Entity> entities = new NativeArray<Entity>(100, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, entities);
        for (int i = 0; i < entities.Length; ++i)
        {
            Entity _entity = entities[i];
            entityManager.SetComponentData(_entity, new LevelComponent() { level = UnityEngine.Random.Range(10, 20) });
        }
        // 释放NativeArray
        entities.Dispose();

        // 创建可渲染entity
        EntityArchetype renderArchetype = entityManager.CreateArchetype(
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(MoveSpeedComponent),
            typeof(LevelComponent));
        NativeArray<Entity> renderEntities = new NativeArray<Entity>(100, Allocator.Temp);
        entityManager.CreateEntity(renderArchetype, renderEntities);
        for (int i = 0; i < renderEntities.Length; ++i)
        {
            Entity _entity = renderEntities[i];
            entityManager.SetComponentData(_entity, new LevelComponent() { level = UnityEngine.Random.Range(10, 20) });
            entityManager.SetComponentData(_entity, new Translation() { Value = new float3(UnityEngine.Random.Range(-20f, 20f), UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)) });
            entityManager.SetComponentData(_entity, new MoveSpeedComponent() { moveSpeed = UnityEngine.Random.Range(-10f, 10f) });
            entityManager.SetSharedComponentData(_entity, new RenderMesh() { mesh = mesh, material = material });
        }
        renderEntities.Dispose();
    }
}