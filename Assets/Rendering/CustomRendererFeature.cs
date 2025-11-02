using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using UnityEngine.Experimental.Rendering;

public class CustomRendererFeature : ScriptableRendererFeature
{
    [SerializeField, Space] private Shader shader;
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    [SerializeField] int passEventOrder = 0;
    [SerializeField, Space] internal RenderFeatureSettings settings;

    internal Material material;
    private CustomRenderFeaturePass customRenderFeaturePass;
    private RTHandle customRenderFeatureRTHandle;
    private bool isInitialized = false;

    public override void Create()
    {
        material = CoreUtils.CreateEngineMaterial(shader);

        customRenderFeaturePass = new CustomRenderFeaturePass(this);
        customRenderFeaturePass.renderPassEvent = (RenderPassEvent)((int)renderPassEvent + passEventOrder);

    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game) return;
        if (customRenderFeatureRTHandle == null || !isInitialized)
        {
            if (customRenderFeatureRTHandle != null)
            {
                customRenderFeatureRTHandle.Release();
                customRenderFeatureRTHandle = null;
            }

            customRenderFeatureRTHandle = RTHandles.Alloc(
                renderingData.cameraData.cameraTargetDescriptor.width,
                renderingData.cameraData.cameraTargetDescriptor.height,
                slices: 1,
                depthBufferBits: DepthBits.None,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Point,
                wrapMode: TextureWrapMode.Clamp,
                dimension: TextureDimension.Tex2D,
                enableRandomWrite: true,
                useMipMap: false,
                autoGenerateMips: false,
                isShadowMap: false,
                anisoLevel: 1,
                useDynamicScale: false,
                name: "CustomRTHandle"
            );

            isInitialized = true;
        }

        customRenderFeaturePass.ConfigureInput(ScriptableRenderPassInput.Color);
        renderer.EnqueuePass(customRenderFeaturePass);
    }
}
public class CustomRenderFeaturePass : ScriptableRenderPass
{
    private readonly CustomRendererFeature rendererFeature;

    private static readonly int gridScaleID = Shader.PropertyToID("_GridScale");

    public CustomRenderFeaturePass(CustomRendererFeature rendererFeature) => this.rendererFeature = rendererFeature;


    private class PassData
    { 
        internal Material material;
        internal TextureHandle sourceColor;
        internal CustomVolumeComponent vc;
        internal TextureHandle targetColor;
        internal RenderFeatureSettings defaultSettings;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        TextureHandle backBufferData = resourceData.backBufferColor;
        TextureDesc textDesc = resourceData.cameraColor.GetDescriptor(renderGraph); //getting exact same texture descriptor from the render graphs frame data
        textDesc.name = "CrunchyDitherGridTexture";
        textDesc.clearBuffer = false;
        TextureHandle dest = renderGraph.CreateTexture(textDesc); //copy of the source camera color for the shader to alter and replace the original camera color

        PassData passData;
        using(var builder = renderGraph.AddRasterRenderPass<PassData>("Crunchy Dither Render Pass", out passData)) //passData is a copy of DitherWorldGridPassData
        {
            passData.targetColor = dest;
            passData.sourceColor = resourceData.cameraColor;
            passData.material = rendererFeature.material;
            passData.vc = VolumeManager.instance.stack.GetComponent<CustomVolumeComponent>();
            passData.defaultSettings = rendererFeature.settings;

            builder.UseTexture(passData.sourceColor); // getting souce color
            builder.SetRenderAttachment(passData.targetColor, 0, AccessFlags.WriteAll); // setting source color via the target that is a copy

            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) => ExecutePass(passData, ctx));
        }
        resourceData.cameraColor = passData.targetColor;
    }

    private static void ExecutePass(PassData passData, RasterGraphContext ctx)
    {
        float gridScale = passData.vc.active && passData.vc.gridScale.overrideState ? passData.vc.gridScale.value : passData.defaultSettings.gridScale;

        passData.material.SetFloat(gridScaleID, gridScale);

        Blitter.BlitTexture(ctx.cmd, passData.sourceColor, Vector2.one, passData.material, 0); // execute shader pass

    }
}

[Serializable]
public class RenderFeatureSettings
{
    [Header("General")]
    [Range(1,4)] public int gridScale = 2;
}


