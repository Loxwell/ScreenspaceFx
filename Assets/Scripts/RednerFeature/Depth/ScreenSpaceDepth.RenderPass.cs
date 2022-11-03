using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using UnityEngine.Experimental.Rendering;

public partial class SSDepthFeature
{

    /// <summary>
    /// Screen-space Depth
    /// </summary>
    class SSDepthRenderPass : ScriptableRenderPass
    {
        const string PROFILER_TAG = "SSDepth-Pass";
        readonly int TEMPORARY_BUFFER_ID = Shader.PropertyToID("_TemporaryBuffer");


        Material m_material;
        RenderTargetIdentifier m_originID;
        RenderTargetIdentifier m_temporaryID;
        PassSettings passSettings;

        internal SSDepthRenderPass(PassSettings passSettings)
        {
            m_material = CoreUtils.CreateEngineMaterial("ScreenSpace/Depth");
            this.passSettings = passSettings;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            //var renderTargetDesc = renderingData.cameraData.cameraTargetDescriptor;
            var renderTargetDesc = renderingData.cameraData.cameraTargetDescriptor;
            renderTargetDesc.depthBufferBits = 16;
            //renderTargetDesc.msaaSamples     = 1;
            //renderTargetDesc.graphicsFormat = RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R8_UNorm, FormatUsage.Linear | FormatUsage.Render)
            //    ? GraphicsFormat.R8_UNorm : GraphicsFormat.B8G8R8A8_UNorm;

            m_originID = renderingData.cameraData.renderer.cameraColorTarget;

            cmd.GetTemporaryRT(TEMPORARY_BUFFER_ID, renderTargetDesc, passSettings.filterMode);
            m_temporaryID = new RenderTargetIdentifier(TEMPORARY_BUFFER_ID);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            using(new ProfilingScope(cmd, new ProfilingSampler(PROFILER_TAG)))
            {
                Blit(cmd, m_originID, m_temporaryID, m_material, 0);
                Blit(cmd, m_temporaryID, m_originID);
                //var cam = renderingData.cameraData.camera;
                //cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                //cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_material);
                //cmd.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);

                //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, false);
                //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, false);
                //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowScreen, true);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd?.ReleaseTemporaryRT(TEMPORARY_BUFFER_ID);
        }

        internal bool OnAddRenderPasses()
        {
            renderPassEvent = passSettings.renderPassEvent;
            //ConfigureInput(ScriptableRenderPassInput.Depth);
            return m_material != null;
        }

        public static implicit operator bool(SSDepthRenderPass rh) => rh?.m_material != null;
    }
}
