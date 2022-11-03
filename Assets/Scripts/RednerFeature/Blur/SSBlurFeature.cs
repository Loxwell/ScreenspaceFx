using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class SSBlurFeature : ScriptableRendererFeature
{
    // References to our pass and its settings.
    public PassSettings passSettings = new();
    RenderPass pass;

    [System.Serializable]
    public class PassSettings
    {

        /// <summary>
        /// Used for any potential down-sampling we will do in the pass.
        /// </summary>
        [Range(1, 4)]
        public int downsample = 1;

        /// <summary>
        /// A variable that's specific to the use case of our pass.
        /// </summary>
        [Range(0, 20)]
        public int blurStrength = 5;

        /// <summary>
        /// Where/when the render pass should be injected during the rendering process.
        /// </summary>
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        // additional properties ...
    }

    /// <summary>
    /// Gets called every time serialization happens.
    /// Gets called when you enable/disable the renderer feature.
    /// Gets called when you change a property in the inspector of the renderer feature.
    /// </summary>
    public override void Create()
    {
        // Pass the settings as a parameter to the constructor of the pass.
        pass = new RenderPass(passSettings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera || !pass) return;

        // Here you can queue up multiple passes after each other.
        renderer.EnqueuePass(pass);
    }
}
