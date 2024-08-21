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
            renderer.Render(context, cameras[i]);
        }
    }
}