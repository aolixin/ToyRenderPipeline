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
        }

        public void Render(ScriptableRenderContext context, Camera camera, ShadowSettings shadowSettings)
        {
            _camera = camera;
            context.SetupCameraProperties(_camera);

            GbufferPass(context);
            context.DrawSkybox(_camera);
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
            }

            LightPass(context);
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
            Material mat = new Material(Shader.Find("ToyRP/lightpass"));
            buffer.Blit(gbufferID[0], BuiltinRenderTextureType.CameraTarget, mat);

            context.ExecuteCommandBuffer(buffer);
            context.Submit();
            buffer.Clear();
        }
    }
}