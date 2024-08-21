using System.Collections;
using System.Collections.Generic;
using ToyRP.runtime;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class ToyRenderPipeline : RenderPipeline
{
    ShadowSettings shadowSettings;


    public Cubemap diffuseIBL;
    public Cubemap specularIBL;
    public Texture brdfLut;

    private ToyCameraRenderer renderer;

    public ToyRenderPipeline( ShadowSettings shadowSettings,ref Cubemap diffuseIBL,ref Cubemap specularIBL,ref Texture  brdfLut)
    {
        this.shadowSettings = shadowSettings;

        GraphicsSettings.lightsUseLinearIntensity = true;

        renderer = new ToyCameraRenderer(shadowSettings,ref diffuseIBL, ref specularIBL, ref brdfLut);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        // 创建并调度命令以清除当前渲染目标
        var cmd = new CommandBuffer();
        //cmd.ClearRenderTarget(true, true, Color.black);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // 指示可编程渲染上下文告诉图形 API 执行调度的命令
        context.Submit();
    }


    protected override void Render(
        ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            renderer.Render(context, cameras[i]);
        }
    }
}