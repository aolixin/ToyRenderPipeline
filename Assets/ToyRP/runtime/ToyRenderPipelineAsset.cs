using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;



[CreateAssetMenu(menuName = "Rendering/ToyRenderPipeline")]
public class ToyRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    ShadowSettings shadows = default;

    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    protected override RenderPipeline CreatePipeline()
    {
        return new ToyRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows);
    }
}
