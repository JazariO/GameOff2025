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
    public RenderTexture birdMaskRenderTexture;
    public RenderTexture photographicRenderTexture;
    public LayerMask birdLayerMask;
    public Material birdMaskMaterial;

    private BirdMaskRenderFeaturePass _birdMaskPass;
    public override void Create()
    {
        _birdMaskPass = new BirdMaskRenderFeaturePass(this)
        {
            // Configures where the render pass should be injected.
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Camera cam = renderingData.cameraData.camera;

        // Only inject for the PhotographicCamera
        if (cam.CompareTag("PhotographicCamera"))
        {
            renderer.EnqueuePass(_birdMaskPass);
        }
    }
}
public class BirdMaskRenderFeaturePass : ScriptableRenderPass
{
    private readonly CustomRendererFeature _rendererFeature;
    public BirdMaskRenderFeaturePass(CustomRendererFeature avatarRenderer)
    {
        _rendererFeature = avatarRenderer;
    }


    private class BirdMaskPassData
    {
        internal RendererListHandle objectRendererList;
    }

    private class PhotographicPassData
    {
        internal RendererListHandle objectRendererList;
    }
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // Get data needed later from the frame
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        if (!resourceData.cameraColor.IsValid()) return; // occasionally in editor switching to a different window or tab will invalidate the camera color textures

        // Use the current camera color and depth descriptions so we don't get mismatches, which can cause validation errors.
        TextureDesc birdMaskTexDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
        birdMaskTexDesc.name = "Bird Mask Render Texture";
        birdMaskTexDesc.clearBuffer = true;
        RTHandle birdMaskRTHandle = RTHandles.Alloc(_rendererFeature.birdMaskRenderTexture);
        TextureHandle birdMaskTexHandle = renderGraph.ImportTexture(birdMaskRTHandle);

        TextureDesc photographicTexDesc = resourceData.cameraColor.GetDescriptor(renderGraph);
        photographicTexDesc.name = "Photographic Render Texture";
        photographicTexDesc.clearBuffer = true;
        TextureHandle photographicTexHandle = renderGraph.CreateTexture(photographicTexDesc);

        // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
        using (var builder = renderGraph.AddRasterRenderPass<BirdMaskPassData>("Bird Mask render pass", out var passData))
        {
            if (_rendererFeature.birdMaskRenderTexture == null || _rendererFeature.birdMaskMaterial == null) return;

            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.opaque, _rendererFeature.birdLayerMask);

            // Redraw only objects that have their LightMode tag set to UniversalForward or SRPDefaultUnlit
            List<ShaderTagId> shadersToOverride = new List<ShaderTagId>
            {
                new("UniversalForward"),
                new("SRPDefaultUnlit")
            };
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
            drawSettings.overrideMaterial = _rendererFeature.birdMaskMaterial;

            // Create the list of objects to draw
            RendererListParams rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

            // Convert the list to a list handle that the render graph system can use
            passData.objectRendererList = renderGraph.CreateRendererList(rendererListParameters);

            // Set the render target as the color and depth textures of the active camera texture
            builder.UseRendererList(passData.objectRendererList);

            builder.SetRenderAttachment(birdMaskTexHandle, 0);
            builder.SetRenderFunc((BirdMaskPassData data, RasterGraphContext context) => ExecuteBirdMaskPass(data, context));
        }
    }

    static void ExecuteBirdMaskPass(BirdMaskPassData data, RasterGraphContext context)
    {
        // Clear the render target to black
        context.cmd.ClearRenderTarget(true, true, Color.clear);

        // Draw the objects in the list
        context.cmd.DrawRendererList(data.objectRendererList);
    }

    static void ExecutePhotographicPass(PhotographicPassData data, RasterGraphContext context)
    {
        // Clear the render target to black
        context.cmd.ClearRenderTarget(true, true, Color.clear);

        // Draw the objects in the list
        context.cmd.DrawRendererList(data.objectRendererList);
    }
}