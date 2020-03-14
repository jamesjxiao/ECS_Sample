using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

[DisableAutoCreation]
public class MoveSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref Translation translation) =>
        {
            // float moveSpeed = 1f;
            // translation.Value.y += moveSpeed * UnityEngine.Time.deltaTime;
        });
    }
}

[DisableAutoCreation]
public class RotateSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref Rotation rotation) =>
        {
            // rotation.Value = Quaternion.Euler(0f, 0f, 100 * UnityEngine.Time.realtimeSinceStartup);
        });
    }
}

[DisableAutoCreation]
public class ScaleSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref NonUniformScale scale) =>
        {
            // scale.Value += 1f * UnityEngine.Time.deltaTime;
        });
    }
}

public class ECS_Sprite : MonoBehaviour
{
    public Mesh mesh;
    public Material material;

    void Start()
    {
        mesh = CreateMesh(1, 1);

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(Rotation),
            typeof(NonUniformScale),
            typeof(RenderMesh));

        NativeArray<Entity> entities = new NativeArray<Entity>(1000, Allocator.Temp);
        
        entityManager.CreateEntity(archetype, entities);

        for(int i = 0; i < entities.Length; ++i)
        {
            Entity entity = entities[i];
            entityManager.SetComponentData(entity, new Translation() { Value = new float3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), 0f)});
            entityManager.SetComponentData(entity, new NonUniformScale(){Value = new float3(1f, 1f, 1f)});
            entityManager.SetSharedComponentData(entity, new RenderMesh()
            {
                mesh = mesh,
                material = material,
            });
        }
    }

    private Mesh CreateMesh(float width, float height)
    {
        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        vertices[0] = new Vector3(-halfWidth, -halfHeight);
        vertices[1] = new Vector3(-halfWidth, halfHeight);
        vertices[2] = new Vector3(halfWidth, halfHeight);
        vertices[3] = new Vector3(halfWidth, -halfHeight);

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(0, 1);
        uv[2] = new Vector2(1, 1);
        uv[3] = new Vector2(1, 0);

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 3;

        triangles[3] = 1;
        triangles[4] = 2;
        triangles[5] = 3;

        Mesh mesh = new Mesh()
        {
            vertices = vertices,
            uv = uv,
            triangles = triangles,
        };

        return mesh;
    }
}
