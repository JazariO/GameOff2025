using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class CustomRendererFeature : ScriptableRendererFeature
{
    [SerializeField, Space] private Shader shader;
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    [SerializeField] int passEventOrder = 0;

    public LayerMask birdMask;
    public ShaderTagSettings shaderTagSettings = new ShaderTagSettings();
    public RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
    public SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;
    public RenderTexture photographicRenderTexture;
    public RenderTexture birdMaskRenderTexture;
    internal Material material;

    private BirdMaskRenderFeaturePass birdMaskRenderFeaturePass;

    public override void Create()
    {
        //material = CoreUtils.CreateEngineMaterial(shader);

        birdMaskRenderFeaturePass = new BirdMaskRenderFeaturePass(this);
        birdMaskRenderFeaturePass.renderPassEvent = (RenderPassEvent)((int)renderPassEvent + passEventOrder);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera.tag != "PhotographicCamera") return;

        birdMaskRenderFeaturePass.SetUp(birdMask.value, shaderTagSettings.GetShaderTagIds(), renderQueueRange, sortingCriteria);

        birdMaskRenderFeaturePass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        renderer.EnqueuePass(birdMaskRenderFeaturePass);
    }
}
public class BirdMaskRenderFeaturePass : ScriptableRenderPass
{
    LayerMask birdLayerMask;
    ShaderTagId[] shaderTagIds;
    RenderQueueRange renderQueueRange;
    SortingCriteria sortingCriteria;

    public void SetUp(LayerMask mask, ShaderTagId[] shaderTagIds = null, RenderQueueRange queueRange = default, SortingCriteria crit = SortingCriteria.CommonOpaque)
    {
        this.birdLayerMask = mask;
        this.shaderTagIds = shaderTagIds;
        this.renderQueueRange = queueRange;
        this.sortingCriteria = crit;
    }
    private readonly CustomRendererFeature rendererFeature;

    public BirdMaskRenderFeaturePass(CustomRendererFeature rendererFeature) => this.rendererFeature = rendererFeature;


    private class PassData
    { 
        internal TextureHandle sourceColor;
        internal TextureHandle targetColor;
        internal TextureHandle birdMaskTexHandle;
        internal RendererListHandle rendererList;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();

        TextureHandle backBufferData = resourceData.backBufferColor;
        TextureDesc textDesc = resourceData.cameraColor.GetDescriptor(renderGraph);
        textDesc.name = "CustomTexture";
        textDesc.clearBuffer = false;
        TextureHandle dest = renderGraph.CreateTexture(textDesc);


        RTHandle birdMaskRTHandle = RTHandles.Alloc(rendererFeature.birdMaskRenderTexture);
        //TextureHandle birdMaskTexHandle = renderGraph.ImportTexture(birdMaskRTHandle);
        PassData passData;
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Bird Mask Render Pass", out passData))
        {

            var rendererListDesc = new RendererListDesc(shaderTagIds, renderingData.cullResults, cameraData.camera)
            {
                sortingCriteria = sortingCriteria,
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = renderQueueRange,
                layerMask = birdLayerMask
            };


           // passData.birdMaskTexHandle = renderGraph.ImportTexture(birdMaskRTHandle);
            passData.targetColor = dest;
            passData.sourceColor = resourceData.cameraColor;
            passData.rendererList = renderGraph.CreateRendererList(rendererListDesc);

            builder.UseRendererList(passData.rendererList);
            builder.UseTexture(passData.sourceColor); // getting souce color
            builder.SetRenderAttachment(passData.targetColor, 0, AccessFlags.WriteAll); // setting source color via the target that is a copy

            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                ctx.cmd.ClearRenderTarget(clearDepth: false, clearColor: true, Color.clear);
                ctx.cmd.DrawRendererList(passData.rendererList);
            });

        }
        resourceData.cameraColor = passData.targetColor;
    }
}

[Serializable] public class ShaderTagSettings
{
    [Flags]
    public enum LightModeTags
    {
        None = 0,
        SRPDefaultUnlit = 1 << 0,
        UniversalForward = 1 << 1,
        UniversalForwardOnly = 1 << 2,
        LightweightForward = 1 << 3,
        DepthNormals = 1 << 4,
        DepthOnly = 1 << 5,
        Standard = SRPDefaultUnlit | UniversalForward | UniversalForwardOnly | LightweightForward,
        All = SRPDefaultUnlit | UniversalForward | UniversalForwardOnly | LightweightForward | DepthNormals | DepthOnly
    }

    public LightModeTags EnabledLightModeTags = LightModeTags.Standard;

    public ShaderTagId[] GetShaderTagIds()
    {
        ShaderTagId[] tags = new ShaderTagId[6];
        if (EnabledLightModeTags.HasFlag(LightModeTags.SRPDefaultUnlit))
        {
            tags[0] = new ShaderTagId("SRPDefaultUnlit");
        }
        if (EnabledLightModeTags.HasFlag(LightModeTags.UniversalForward))
        {
            tags[1] = new ShaderTagId("UniversalForward");
        }
        if (EnabledLightModeTags.HasFlag(LightModeTags.UniversalForwardOnly))
        {
            tags[2] = new ShaderTagId("UniversalForwardOnly");
        }
        if (EnabledLightModeTags.HasFlag(LightModeTags.LightweightForward))
        {
            tags[3] = new ShaderTagId("LightweightForward");
        }
        if (EnabledLightModeTags.HasFlag(LightModeTags.DepthNormals))
        {
            tags[4] = new ShaderTagId("DepthNormals");
        }
        if (EnabledLightModeTags.HasFlag(LightModeTags.DepthOnly))
        {
            tags[5] = new ShaderTagId("DepthOnly");
        }
        return tags;
    }
}


