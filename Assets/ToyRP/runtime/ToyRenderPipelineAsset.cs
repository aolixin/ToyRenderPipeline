using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;



[CreateAssetMenu(menuName = "Rendering/ToyRenderPipeline")]
public class ToyRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    ShadowSettings shadows = default;

    public Cubemap diffuseIBL;
    public Cubemap specularIBL;
    public Texture brdfLut;
    
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    protected override RenderPipeline CreatePipeline()
    {
        var rp = new ToyRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows);
        rp.diffuseIBL = diffuseIBL;
        rp.specularIBL = specularIBL;
        rp.brdfLut = brdfLut;
        return rp;
    }
}
