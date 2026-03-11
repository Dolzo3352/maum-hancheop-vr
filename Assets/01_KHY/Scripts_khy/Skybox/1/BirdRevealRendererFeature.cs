using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// Unity 6 URP RenderGraph 대응 버전
/// RecordRenderGraph 구현으로 경고 없음
/// </summary>
public class BirdRevealRendererFeature : ScriptableRendererFeature
{
    [Tooltip("BirdSkyboxReveal.shader로 만든 Material")]
    public Material overrideMaterial;

    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

    private BirdRevealPass _pass;
    private static readonly int PropRevealProgress = Shader.PropertyToID("_RevealProgress");

    public override void Create()
    {
        _pass = new BirdRevealPass();
        _pass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (overrideMaterial == null) return;

        // 전환 중일 때만 패스 등록
        float progress = Shader.GetGlobalFloat(PropRevealProgress);
        if (progress <= 0.001f || progress >= 0.999f) return;

        _pass.Setup(overrideMaterial);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
    }

    // ── Inner Pass ───────────────────────────────────────────────
    class BirdRevealPass : ScriptableRenderPass
    {
        private Material _material;
        private const string PassName = "BirdSkyboxReveal";

        // RenderGraph용 텍스처 핸들
        private TextureHandle _tempTexture;

        public BirdRevealPass()
        {
            profilingSampler = new ProfilingSampler(PassName);
            // RenderGraph 사용 시 requiresIntermediateTexture 불필요
        }

        public void Setup(Material mat) => _material = mat;

        // ── Unity 6 RenderGraph 방식 ─────────────────────────────
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData   cameraData   = frameData.Get<UniversalCameraData>();

            // Scene / Preview 카메라 제외
            if (cameraData.camera.cameraType == CameraType.Preview ||
                cameraData.camera.cameraType == CameraType.Reflection) return;

            // 현재 카메라 컬러 버퍼
            TextureHandle source = resourceData.activeColorTexture;

            // 임시 텍스처 생성
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            TextureHandle tempTex = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_BirdRevealTemp", false
            );

            // Pass 데이터 구조체
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData))
            {
                passData.material  = _material;
                passData.source    = source;
                passData.tempTex   = tempTex;

                // 소스를 읽기 용도로 등록
                builder.UseTexture(source, AccessFlags.Read);

                // 임시 텍스처를 쓰기 용도로 등록
                builder.SetRenderAttachment(tempTex, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    // source → tempTex (복사)
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }

            // 두 번째 패스: tempTex → source (셰이더 적용)
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName + "_Apply", out var passData))
            {
                passData.material = _material;
                passData.source   = tempTex;
                passData.tempTex  = source;

                builder.UseTexture(tempTex, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }

        // ── Legacy Execute (Compatibility Mode 대응) ─────────────
        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }

        public void Dispose() { }

        // Pass 데이터 전달용 구조체
        class PassData
        {
            public Material       material;
            public TextureHandle  source;
            public TextureHandle  tempTex;
        }
    }
}