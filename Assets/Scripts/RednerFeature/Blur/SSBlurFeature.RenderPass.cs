using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public partial class SSBlurFeature
{
    class RenderPass : ScriptableRenderPass
    {
        /// <summary>
        /// The profiler tag that will show up in the frame debugger.
        /// </summary>
        const string PROFILER_TAG = "SSBlur-Pass";

        readonly int TEMPORARY_BUFFER_ID = Shader.PropertyToID("_TemporaryBuffer");

        /// <summary>
        /// It is good to cache the shader property IDs here.
        /// </summary>
        readonly int BLUR_STRENGTH_PROPERTY = Shader.PropertyToID("_BlurStrength");

        /// <summary>
        /// We will store our pass settings in this variable.  
        /// </summary>
        PassSettings passSettings;
        Material material;

        RenderTargetIdentifier m_colorBuffer, m_temporaryBuffer;

        internal RenderPass(PassSettings passSettings)
        {
            this.passSettings = passSettings;
            renderPassEvent = passSettings.renderPassEvent;

            // We create a material that will be used during our pass. You can do it like this using the 'CreateEngineMaterial' method, giving it
            // a shader path as an input or you can use a 'public Material material;'
            // field in your pass settings and access it here through 'passSettings.material'.
            if (!material)
                material = CoreUtils.CreateEngineMaterial("Hidden/Box Blur");

            // Set any material properties based on our pass settings. 
            material?.SetInt(BLUR_STRENGTH_PROPERTY, passSettings.blurStrength);
        }

        /// <summary>
        /// Gets called by the renderer before executing the pass.
        /// Can be used to configure render targets and their clearing state.
        /// Can be user to create temporary render target textures.
        /// If this method is not overriden, the render pass will render to the active camera render target.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="renderingData"></param>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Grab the camera target descriptor.
            // We will use this when creating a temporary render texture.
            var renderTargetDesc = renderingData.cameraData.cameraTargetDescriptor;

            // Downsample the original camera target descriptor. 
            // You would do this for performance reasons or less commonly, for aesthetics.
            renderTargetDesc.width /= passSettings.downsample;
            renderTargetDesc.height /= passSettings.downsample;

            // Set the number of depth bits we need for our temporary render texture.
            renderTargetDesc.depthBufferBits = 0;


            // Grab the color buffer from the renderer camera color target.
            m_colorBuffer = renderingData.cameraData.renderer.cameraColorTarget;

            // Create a temporary render texture using the descriptor from above.
            cmd.GetTemporaryRT(TEMPORARY_BUFFER_ID, renderTargetDesc, FilterMode.Bilinear);
            m_temporaryBuffer = new RenderTargetIdentifier(TEMPORARY_BUFFER_ID);
        }

        /// <summary>
        /// The actual execution of the pass. This is where custom rendering occurs.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Grab a command buffer. We put the actual execution of the pass inside of a profiling scope.
            var cmd = CommandBufferPool.Get();
            
            using(new ProfilingScope(cmd, new ProfilingSampler(PROFILER_TAG)))
            {
                // Blit from the color buffer to a temporary buffer and back. This is needed for a two-pass shader.
                // After these 2 steps you ended up with a box-blurred camera color buffer.

                //Blit from color to temporary (blurring vertically in the process)
                Blit(cmd, m_colorBuffer, m_temporaryBuffer, material, 0); // shader pass 0
                
                // Blit from temporary to color(blurring horizontally in the process)
                Blit(cmd, m_temporaryBuffer, m_colorBuffer, material, 1); // shader pass 1
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// Called when the camera has finished rendering.
        /// Here we release/cleanup any allocated resources that were created by this pass.
        /// Gets called for all cameras i na camera stack.
        /// </summary>
        /// <param name="cmd"></param>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null) throw new System.Exception("cmd is null");

            // Since we created a temporary render texture in OnCameraSetup, we need to release the memory here to avoid a leak.
            cmd.ReleaseTemporaryRT(TEMPORARY_BUFFER_ID);
        }

        /// <summary>
        /// if your pass requires access to the CameraDepthTexture or the CameraNormalsTexture.
        /// </summary>
        internal void OnAddRenderPasses()
        {
            ConfigureInput(ScriptableRenderPassInput.Depth);
            ConfigureInput(ScriptableRenderPassInput.Normal);
        }

        public static implicit operator bool(RenderPass rh) => rh?.material != null;
    }
}
