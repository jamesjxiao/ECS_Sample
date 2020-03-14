using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

[BurstCompile]
public struct ReallyToughJob : IJob
{
    public void Execute()
    {
        float value = 123f;
        for (int i = 0; i < 50000; ++i)
        {
            value = math.exp10(math.sqrt(value));
        }
    }
}

public class Job_First : MonoBehaviour
{
    public bool useJob = false;

    public void Update()
    {
        float startTime = Time.realtimeSinceStartup;
        if (!useJob)
        {
            for(int i = 0; i < 10; ++i)
            {
                ReallyToughTask();
            }
        }
        else
        {
            NativeArray<JobHandle> jobs = new NativeArray<JobHandle>(10, Allocator.Temp);
            for(int i = 0 ; i < jobs.Length; ++i)
            {
                jobs[i] = ReallyToughTaskJob();
            }
            JobHandle.CompleteAll(jobs);
            jobs.Dispose();
        }
    }

    private void ReallyToughTask()
    {
        float value = 123f;
        for (int i = 0; i < 50000; ++i)
        {
            value = math.exp10(math.sqrt(value));
        }
    }

    private JobHandle ReallyToughTaskJob()
    {
        ReallyToughJob job = new ReallyToughJob();
        return job.Schedule();
    }
}
