using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public struct WritePartOfArrayJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> source;
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<float> dest;
    public int startIndex;

    public void Execute(int index)
    {
        int idx = startIndex + index;
        dest[idx] = source[index];
    }
}

public class Job_CombineWriting : MonoBehaviour
{
    private void Start()
    {
        NativeArray<float> array1 = new NativeArray<float>(5, Allocator.TempJob);
        NativeArray<float> array2 = new NativeArray<float>(5, Allocator.TempJob);
        for (int i = 0; i < 5; ++i)
        {
            array1[i] = i;
            array2[i] = i + 5;
        }

        NativeArray<float> arrayCombine = new NativeArray<float>(10, Allocator.TempJob);
        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(2, Allocator.TempJob);
        WritePartOfArrayJob job1 = new WritePartOfArrayJob()
        {
            source = array1,
            dest = arrayCombine,
            startIndex = 0,
        };
        handles[0] = job1.Schedule(array1.Length, 1);

        WritePartOfArrayJob job2 = new WritePartOfArrayJob()
        {
            source = array2,
            dest = arrayCombine,
            startIndex = 5,
        };
        handles[1] = job2.Schedule(array2.Length, 1);
        JobHandle.CompleteAll(handles);

        for(int i = 0; i < arrayCombine.Length; ++i)
        {
            Debug.Log(arrayCombine[i]);
        }

        array1.Dispose();
        array2.Dispose();
        arrayCombine.Dispose();
    }
}