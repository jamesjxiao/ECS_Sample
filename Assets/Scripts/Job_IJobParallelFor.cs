using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;

[BurstCompile]
public struct TransformJob : IJobParallelFor
{
    public NativeArray<float3> positions;
    public NativeArray<float> moveSpeeds;
    [ReadOnly] public float deltaTime;

    public void Execute(int index)
    {
        positions[index] = positions[index] + new float3(0f, moveSpeeds[index] * deltaTime, 0f);

        if (positions[index].y > 20f)
            moveSpeeds[index] = -Mathf.Abs(moveSpeeds[index]);
        else if (positions[index].y < -20f)
            moveSpeeds[index] = Mathf.Abs(moveSpeeds[index]);
    }
}

public class Job_IJobParallelFor : MonoBehaviour
{
    public bool useJob = false;
    public Transform player;

    public class Player
    {
        public Transform transform;
        public float moveSpeed;
    }
    private List<Player> players;

    private void Start()
    {
        players = new List<Player>();

        for(int i = 0; i < 1000; ++i)
        {
            Player p = new Player();
            p.transform = GameObject.Instantiate(player);
            p.transform.position = new Vector3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), 0f);
            p.transform.rotation = Quaternion.identity;
            p.moveSpeed = UnityEngine.Random.Range(-5f, 5f);
            players.Add(p);
        }
    }

    private void Update()
    {
        if(useJob == false)
        {
            for(int i = 0; i < players.Count; ++i)
            {
                var p = players[i];
                p.transform.position += new Vector3(0f, p.moveSpeed * UnityEngine.Time.deltaTime, 0f);

                if (p.transform.position.y > 20f)
                    p.moveSpeed = -Mathf.Abs(p.moveSpeed);
                else if (p.transform.position.y < -20f)
                    p.moveSpeed = Mathf.Abs(p.moveSpeed);
            }
        }
        else
        {
            NativeArray<float3> _positions = new NativeArray<float3>(players.Count, Allocator.TempJob);
            NativeArray<float> _moveSpeeds = new NativeArray<float>(players.Count, Allocator.TempJob);

            for(int i = 0; i < players.Count; ++i)
            {
                _positions[i] = players[i].transform.position;
                _moveSpeeds[i] = players[i].moveSpeed;
            }

            TransformJob job = new TransformJob()
            {
                positions = _positions,
                moveSpeeds = _moveSpeeds,
                deltaTime = UnityEngine.Time.deltaTime,
            };
            JobHandle handle = job.Schedule(players.Count, 1000);
            handle.Complete();

            for(int i = 0; i < players.Count; ++i)
            {
                players[i].transform.position = _positions[i];
                players[i].moveSpeed = _moveSpeeds[i];
            }

            _positions.Dispose();
            _moveSpeeds.Dispose();
        }
    }

}
