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
    // 对象类型tag
    public struct QuardrantType : IComponentData
    {
        public enum TypeEnum { Unit, Target}
        public TypeEnum type;
    }

    // 象限数据
    public struct QuardrantData : IComponentData
    {
        public Entity entity;
        public float3 position;
        public QuardrantType quardrantEntity;
    }

}
