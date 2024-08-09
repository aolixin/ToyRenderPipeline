using System.Collections;
using System.Collections.Generic;
using ToyRP.runtime;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class ToyRenderPipeline : RenderPipeline
{
    bool useDynamicBatching, useGPUInstancing;
    ShadowSettings shadowSettings;

        
    public Cubemap diffuseIBL;
    public Cubemap specularIBL;
    public Texture brdfLut;
    
    public ToyRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,
        ShadowSettings shadowSettings)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;

        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }
    ToyCameraRenderer renderer = new ToyCameraRenderer();

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        // ���������������������ǰ��ȾĿ��
        var cmd = new CommandBuffer();
        //cmd.ClearRenderTarget(true, true, Color.black);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // ָʾ�ɱ����Ⱦ�����ĸ���ͼ�� API ִ�е��ȵ�����
        context.Submit();
    }


    protected override void Render(
    ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            renderer.Render(context, cameras[i],ref diffuseIBL,ref specularIBL,ref brdfLut);
        }
    }
}
