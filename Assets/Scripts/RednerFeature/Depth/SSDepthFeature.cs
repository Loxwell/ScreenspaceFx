using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.XR.XRDisplaySubsystem;

public partial class SSDepthFeature : ScriptableRendererFeature
{
    SSDepthRenderPass m_renderPass;

    [SerializeField]
    PassSettings passSettings;

    [System.Serializable]
    public class PassSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public FilterMode filterMode = FilterMode.Bilinear;
    }

    public override void Create()
    {
        if (m_renderPass == null)
        {
            m_renderPass = new SSDepthRenderPass(passSettings);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
#if UNITY_EDITOR
        if (renderingData.cameraData.isSceneViewCamera) return;
#endif
        if (!m_renderPass) return;

        m_renderPass.OnAddRenderPasses();
        renderer.EnqueuePass(m_renderPass);
    }
}