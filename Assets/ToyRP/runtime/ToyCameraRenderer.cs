using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.VirtualTexturing;

namespace ToyRP.runtime
{
    public class ToyCameraRenderer
    {
        public const string _defaulBuffer = "Render Camera";
        public const string _gbufferPass = "gbuffer";
        public const string _lightPass = "lightpass";


        private Camera _camera;
        private CommandBuffer buffer;


        RenderTexture gdepth; // depth attachment
        RenderTexture[] gbuffers = new RenderTexture[4]; // color attachments 
        RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[4]; // tex ID

        public Cubemap _diffuseIBL;
        public Cubemap _specularIBL;
        public Texture _brdfLut;

        // 阴影管理
        public int shadowMapResolution = 1024;
        CSM csm;
        RenderTexture[] shadowTextures = new RenderTexture[4]; // 阴影贴图

        public ToyCameraRenderer()
        {
            buffer = new CommandBuffer();
            buffer.name = _defaulBuffer;

            // 创建纹理
            gdepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth,
                RenderTextureReadWrite.Linear);
            gbuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            gbuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010,
                RenderTextureReadWrite.Linear);
            gbuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64,
                RenderTextureReadWrite.Linear);
            gbuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);

            // 给纹理 ID 赋值
            for (int i = 0; i < 4; i++)
                gbufferID[i] = gbuffers[i];

            // 设置 gbuffer 为全局纹理
            buffer.SetGlobalTexture("_gdepth", gdepth);
            for (int i = 0; i < 4; i++)
                buffer.SetGlobalTexture("_GT" + i, gbuffers[i]);

            // 创建阴影贴图
            for (int i = 0; i < 4; i++)
                shadowTextures[i] = new RenderTexture(shadowMapResolution, shadowMapResolution, 24,
                    RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);

            csm = new CSM();
            // 设置阴影贴图
            for (int i = 0; i < 4; i++)
            {
                Shader.SetGlobalTexture("_shadowtex" + i, shadowTextures[i]);
                Shader.SetGlobalFloat("_split" + i, csm.splts[i]);
            }
            
        }

        public void Render(ScriptableRenderContext context, Camera camera, ref Cubemap diffuseIBL,
            ref Cubemap specularIBL, ref Texture brdfLut)
        {
            _camera = camera;
            _diffuseIBL = diffuseIBL;
            _specularIBL = specularIBL;
            _brdfLut = brdfLut;
            ShadowPass(context);

            context.SetupCameraProperties(_camera);
            GbufferPass(context);

            LightPass(context);

            context.DrawSkybox(_camera);
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
            }

            context.Submit();
        }

        void GbufferPass(ScriptableRenderContext context)
        {
            buffer.name = _gbufferPass;
            buffer.SetRenderTarget(gbufferID, gdepth);
            buffer.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(buffer);

            _camera.TryGetCullingParameters(out var cullingParameters);
            var cullingResults = context.Cull(ref cullingParameters);

            ShaderTagId shaderTagId = new ShaderTagId("gbuffer");
            var sortingSettings = new SortingSettings(_camera);
            var drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            var filteringSettings = FilteringSettings.defaultValue;


            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            context.Submit();
            buffer.Clear();
        }

        void LightPass(ScriptableRenderContext context)
        {
            buffer.name = _lightPass;

            // set matrix
            // 设置相机矩阵
            Matrix4x4 viewMatrix = _camera.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false);
            Matrix4x4 vpMatrix = projMatrix * viewMatrix;
            Matrix4x4 vpMatrixInv = vpMatrix.inverse;
            buffer.SetGlobalMatrix("_vpMatrix", vpMatrix);
            buffer.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);

            buffer.SetGlobalTexture("_diffuseIBL", _diffuseIBL);
            buffer.SetGlobalTexture("_specularIBL", _specularIBL);
            buffer.SetGlobalTexture("_brdfLut", _brdfLut);


            Material mat = new Material(Shader.Find("ToyRP/lightpass"));
            buffer.Blit(gbufferID[0], BuiltinRenderTextureType.CameraTarget, mat);

            context.ExecuteCommandBuffer(buffer);
            context.Submit();
            buffer.Clear();
        }


        // 阴影贴图 pass
        void ShadowPass(ScriptableRenderContext context)
        {
 

            // 获取光源信息
            Light light = RenderSettings.sun;
            // Debug.Log(light.color);
            Vector3 lightDir = light.transform.rotation * Vector3.forward;

            // 更新 shadowmap 分割
            csm.Update(_camera, lightDir);

            csm.SaveMainCameraSettings(ref _camera);
            for (int level = 0; level < 4; level++)
            {
                // 将相机移到光源方向
                csm.ConfigCameraToShadowSpace(ref _camera, lightDir, level, 500.0f);
                
                Matrix4x4 v = _camera.worldToCameraMatrix;
                Matrix4x4 p = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false);
                Shader.SetGlobalMatrix("_shadowVpMatrix" + level, p * v);

                buffer.name = "shadowmap" + level;

                // 绘制前准备
                context.SetupCameraProperties(_camera);
                buffer.SetRenderTarget(shadowTextures[level]);
                buffer.ClearRenderTarget(true, true, Color.green);
                context.ExecuteCommandBuffer(buffer);

                // 剔除
                _camera.TryGetCullingParameters(out var cullingParameters);
                var cullingResults = context.Cull(ref cullingParameters);
                // config settings
                ShaderTagId shaderTagId = new ShaderTagId("depthonly");
                SortingSettings sortingSettings = new SortingSettings(_camera);
                DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
                FilteringSettings filteringSettings = FilteringSettings.defaultValue;

                // 绘制
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

                context.Submit(); // 每次 set camera 之后立即提交
                buffer.Clear();
                
                
                
            }

            csm.RevertMainCameraSettings(ref _camera);
            // Debug.Log(_camera.transform.position);

            // for (int level = 0; level < 4; level++)
            // {
            //     Matrix4x4 v = _camera.worldToCameraMatrix;
            //     Matrix4x4 p = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false);
            //     Shader.SetGlobalMatrix("_shadowVpMatrix" + level, p * v);
            // }
        }
    }
}