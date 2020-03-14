using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

public struct SimpleJob : IJob
{
    public int a;
    public int b;
    public NativeArray<int> result;

    public void Execute()
    {
        result[0] = a + b;
    }
}

public class Job_GetResult : MonoBehaviour
{
    private void Start()
    {
        NativeArray<int> array = new NativeArray<int>(1, Allocator.TempJob);

        SimpleJob job = new SimpleJob()
        {
            a = 1,
            b = 2,
            result = array,
        };

        JobHandle handle = job.Schedule();
        handle.Complete();

        Debug.Log(job.result[0]);
        array.Dispose();
    }

}
