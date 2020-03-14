using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

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
