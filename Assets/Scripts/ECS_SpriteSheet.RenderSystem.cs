using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using Unity.Collections;

namespace ECS_SpriteSheet
{
    [UpdateAfter(typeof(SpriteSheetAnimationSystem))]
    [DisableAutoCreation]
    public class SpriteSheetRenderer : ComponentSystem
    {
        private int ID_MainTex_UV;
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        protected override void OnCreate()
        {
            base.OnCreate();
            ID_MainTex_UV = Shader.PropertyToID("_MainTex_UV");
        }

        protected override void OnUpdate()
        {
            EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<SpriteSheetAnimationData>(), ComponentType.ReadOnly<Translation>());

            NativeArray<SpriteSheetAnimationData> anims = query.ToComponentDataArray<SpriteSheetAnimationData>(Allocator.TempJob);
            NativeArray<Translation> translations = query.ToComponentDataArray<Translation>(Allocator.TempJob);
            if (anims.Length <= 0)
                return;

            //// 根据Y值，从上往下绘制
            //for (int i = 0; i < anims.Length; ++i)
            //{
            //    for (int j = i + 1; j < anims.Length; ++j)
            //    {
            //        if (translations[i].Value.y < translations[j].Value.y)
            //        {
            //            var tmpTranslation = translations[i];
            //            translations[i] = translations[j];
            //            translations[j] = tmpTranslation;

            //            var tmpAnims = anims[i];
            //            anims[i] = anims[j];
            //            anims[j] = tmpAnims;
            //        }
            //    }
            //}
            //translations.Dispose();

            // 一次最多绘制1023个单位
            int totalNum = anims.Length;
            int sliceNum = 1023;
            int groupNum = totalNum / sliceNum + (totalNum % sliceNum > 0 ? 1 : 0);
            for (int i = 0; i < groupNum; ++i)
            {
                List<Matrix4x4> matrixs = new List<Matrix4x4>();
                List<Vector4> uvs = new List<Vector4>();

                int begin = i * sliceNum;
                int end = Mathf.Min((i + 1) * sliceNum, totalNum);
                for (int j = begin; j < end; ++j)
                {
                    matrixs.Add(anims[j].matrix);
                    uvs.Add(anims[j].uv);
                }
                mpb.SetVectorArray(ID_MainTex_UV, uvs);
                Graphics.DrawMeshInstanced(ECS_SpriteSheet.Instance.mesh, 0, ECS_SpriteSheet.Instance.materal, matrixs.ToArray(), end - begin, mpb);
            }
            anims.Dispose();
        }

        protected void OnUpdate_old_1023()
        {
            EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<SpriteSheetAnimationData>());

            NativeArray<SpriteSheetAnimationData> anims = query.ToComponentDataArray<SpriteSheetAnimationData>(Allocator.TempJob);

            List<Matrix4x4> matrixs = new List<Matrix4x4>();
            List<Vector4> uvs = new List<Vector4>();
            for(int i =0;i < anims.Length; ++i)
            {
                matrixs.Add(anims[i].matrix);
                uvs.Add(anims[i].uv);
            }
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetVectorArray(ID_MainTex_UV, uvs);

            Graphics.DrawMeshInstanced(ECS_SpriteSheet.Instance.mesh, 0, ECS_SpriteSheet.Instance.materal, matrixs, mpb);
            anims.Dispose();
        }

        protected void OnUpdate_old()
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();

            Entities.ForEach((ref SpriteSheetAnimationData animData) =>
            {
                mpb.SetVector(ID_MainTex_UV, animData.uv);
                // 使用T R S绘制
                //Graphics.DrawMesh(ECS_SpriteSheet.Instance.mesh, translation.Value, Quaternion.identity, ECS_SpriteSheet.Instance.materal, 0,
                // Camera.main, 0, mpb);

                // 使用Matrix绘制，Scene不绘制
                Graphics.DrawMesh(ECS_SpriteSheet.Instance.mesh, animData.matrix, ECS_SpriteSheet.Instance.materal, 0, ECS_SpriteSheet.Instance.randerCamera, 0, mpb);
            });
        }
    }
}