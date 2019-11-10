using UnityEngine;
using UnityEngine.Rendering;

[ImageEffectAllowedInSceneView]
[ExecuteInEditMode()]
public class NGSS_FrustumShadows : MonoBehaviour
{
    [Header("REFERENCES")]
    public Light mainShadowsLight;
    public Shader frustumShadowsShader;

    [Header("SHADOWS SETTINGS")]
    [Tooltip("Poisson Noise. Randomize samples to remove repeated patterns.")]
    public bool m_dithering = false;

    [Tooltip("If enabled a faster separable blur will be used.\nIf disabled a slower depth aware blur will be used.")]
    public bool m_fastBlur = true;

    [Tooltip("If enabled, backfaced lit fragments will be skipped increasing performance. Requires GBuffer normals.")]
    public bool m_deferredBackfaceOptimization = false;

    [Range(0f, 1f), Tooltip("Set how backfaced lit fragments are shaded. Requires DeferredBackfaceOptimization to be enabled.")]
    public float m_deferredBackfaceTranslucency = 0f;

    [Tooltip("Tweak this value to remove soft-shadows leaking around edges.")]
    [Range(0.01f, 1f)]
    public float m_shadowsEdgeBlur = 0.25f;

    [Tooltip("Overall softness of the shadows.")]
    [Range(0.01f, 1.0f)]
    public float m_shadowsBlur = 0.5f;

    //[Tooltip("The distance where shadows start to fade.")]
    //[Range(0.1f, 4.0f)]
    //public float m_shadowsFade = 1f;

    [Tooltip("Tweak this value if your objects display backface shadows.")]
    [Range(0.0f, 1f)]
    public float m_shadowsBias = 0.05f;

#if !UNITY_5
    [Tooltip("The distance in metters from camera where shadows start to shown.")]
    [Min(0f)]
    public float m_shadowsDistanceStart = 0f;
#else
    [Tooltip("The distance in metters from camera where shadows start to shown.")]
    public float m_shadowsDistanceStart = 0f;
#endif
    [Header("RAY SETTINGS")]
    [Tooltip("If enabled the ray length will be scaled at screen space instead of world space. Keep it enabled for an infinite view shadows coverage. Disable it for a ContactShadows like effect. Adjust the Ray Scale property accordingly.")]
    public bool m_rayScreenScale = true;

    [Tooltip("Number of samplers between each step. The higher values produces less gaps between shadows but is more costly.")]
    [Range(16, 128)]
    public int m_raySamples = 64;

    [Tooltip("The higher the value, the larger the shadows ray will be.")]
    [Range(0.01f, 1f)]
    public float m_rayScale = 0.25f;

    [Tooltip("The higher the value, the ticker the shadows will look.")]
    [Range(0.0f, 1.0f)]
    public float m_rayThickness = 0.01f;

    [Header("TEMPORAL SETTINGS (EXPERIMENTAL)")]
    [Tooltip("Temporal filtering. Improves the shadows aliasing by adding an extra temporal pass. Currently experimental, does not work well with Scene View open.")]
    public bool m_Temporal = false;
    private bool isTemporal = false;
    [Range(0f, 1f)]
    [Tooltip("Temporal scale in seconds. The bigger the smoother the shadows but produces trail/blur within shadows.")]
    public float m_Scale = 0.75f;
    [Tooltip("Improves the temporal filter by shaking the screen space shadows at different frames.")]
    [Range(0f, 0.25f)]
    public float m_Jittering = 0f;

    /*******************************************************************************************************************/

    //private Texture2D noMixTexture;    

    private int m_SampleIndex = 0;

    private CommandBuffer computeShadowsCB;
    private bool isInitialized = false;

    bool IsNotSupported()
    {
#if UNITY_2018_1_OR_NEWER
        return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2);
#elif UNITY_2017_4_OR_EARLIER
        return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStationVita || SystemInfo.graphicsDeviceType == GraphicsDeviceType.N3DS);
#else
        return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D9 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStationMobile || SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStationVita || SystemInfo.graphicsDeviceType == GraphicsDeviceType.N3DS);
#endif
    }

    private RenderTexture mTempRT;
    private RenderTexture TempRT
    {
        get
        {
            if (mTempRT == null || (mTempRT.width != mCamera.pixelWidth || mTempRT.height != mCamera.pixelHeight))
            {
                if (mTempRT) { RenderTexture.ReleaseTemporary(mTempRT); }

                mTempRT = RenderTexture.GetTemporary(mCamera.pixelWidth, mCamera.pixelHeight, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
                mTempRT.hideFlags = HideFlags.HideAndDontSave;
            }
            return mTempRT;
        }
        set { mTempRT = value; }
    }

    private Camera _mCamera;
    private Camera mCamera
    {
        get
        {
            if (_mCamera == null)
            {
                _mCamera = GetComponent<Camera>();
                if (_mCamera == null) { _mCamera = Camera.main; }
                if (_mCamera == null) { Debug.LogError("NGSS Error: No MainCamera found, please provide one.", this); enabled = false; }
                //#if UNITY_EDITOR
                //if (UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera != null)
                //UnityEditor.SceneView.currentDrawingSceneView.camera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
                //#endif

                //MonoBehaviour ngss_cs = _mCamera.GetComponent("NGSS_ContactShadows") as MonoBehaviour;
                //if (ngss_cs != null) { ngss_cs.enabled = false; DestroyImmediate(ngss_cs); }
            }
            return _mCamera;
        }
    }

    private Material _mMaterial;
    private Material mMaterial
    {
        get
        {
            if (_mMaterial == null)
            {
                //_mMaterial = new Material(Shader.Find("Hidden/NGSS_FrustumShadows"));//Automatic (sometimes it bugs)
                if (frustumShadowsShader == null) { frustumShadowsShader = Shader.Find("Hidden/NGSS_FrustumShadows"); }
                _mMaterial = new Material(frustumShadowsShader);//Manual
                if (_mMaterial == null) { Debug.LogWarning("NGSS Warning: can't find NGSS_FrustumShadows shader, make sure it's on your project.", this); enabled = false; }
            }
            return _mMaterial;
        }
    }

    void AddCommandBuffers()
    {
        if (computeShadowsCB == null) { computeShadowsCB = new CommandBuffer { name = "NGSS FrustumShadows: Compute" }; } else { computeShadowsCB.Clear(); }

        bool canAddBuff = true;
        foreach (CommandBuffer cb in mCamera.GetCommandBuffers(m_Temporal ? CameraEvent.AfterGBuffer : CameraEvent.BeforeLighting)) { if (cb.name == computeShadowsCB.name) { canAddBuff = false; break; } }
        if (canAddBuff) { mCamera.AddCommandBuffer(m_Temporal ? CameraEvent.AfterGBuffer : CameraEvent.BeforeLighting, computeShadowsCB); }
        /*
#if UNITY_EDITOR
        if (UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera != null)
        {
            UnityEditor.SceneView.currentDrawingSceneView.camera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            UnityEditor.SceneView.currentDrawingSceneView.camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, computeShadowsCB);
        }
#endif*/
    }

    void RemoveCommandBuffers()
    {
        _mMaterial = null;
        if (mCamera) { mCamera.RemoveCommandBuffer(isTemporal ? CameraEvent.AfterGBuffer : CameraEvent.BeforeLighting, computeShadowsCB); }
        //We dont need this anymore as the contact shadows mix is done directly on shadow internal files
        /*
#if UNITY_EDITOR
        if (UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera != null)
            UnityEditor.SceneView.currentDrawingSceneView.camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, computeShadowsCB);
#endif*/

        isInitialized = false;
    }

    void Init()
    {
        if (isInitialized || mainShadowsLight == null) { return; }

        //comment me these 3 lines if sampling directly from internal files
        //noMixTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        //noMixTexture.SetPixel(0, 0, Color.white);
        //noMixTexture.Apply();

        if (mCamera.actualRenderingPath == RenderingPath.VertexLit)
        {
            Debug.LogWarning("Vertex Lit Rendering Path is not supported by NGSS Contact Shadows. Please set the Rendering Path in your game camera or Graphics Settings to something else than Vertex Lit.", this);
            enabled = false;
            //DestroyImmediate(this);
            return;
        }

        AddCommandBuffers();

        int cShadow = Shader.PropertyToID("NGSS_ContactShadowRT");
        int cShadow2 = Shader.PropertyToID("NGSS_ContactShadowRT2");
        int dSource = Shader.PropertyToID("NGSS_DepthSourceRT");

        computeShadowsCB.GetTemporaryRT(cShadow, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.R8);
        computeShadowsCB.GetTemporaryRT(cShadow2, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.R8);
        computeShadowsCB.GetTemporaryRT(dSource, -1, -1, 0, FilterMode.Point, RenderTextureFormat.RFloat);
        //computeShadowsCB.SetGlobalTexture(Shader.PropertyToID("ScreenSpaceMask"), BuiltinRenderTextureType.CurrentActive);//requires a commandBuffer on the light, not compatible with local light

        computeShadowsCB.Blit(cShadow, dSource, mMaterial, 0);//clip edges
        computeShadowsCB.Blit(dSource, cShadow, mMaterial, 1);//compute ssrt shadows

        //blur shadows
        computeShadowsCB.SetGlobalVector("ShadowsKernel", new Vector2(0.0f, 1.0f));
        computeShadowsCB.Blit(cShadow, cShadow2, mMaterial, 2);
        computeShadowsCB.SetGlobalVector("ShadowsKernel", new Vector2(1.0f, 0.0f));
        computeShadowsCB.Blit(cShadow2, cShadow, mMaterial, 2);

        //temporal
        if (m_Temporal)
        {
            //TempRT = RenderTexture.GetTemporary(mCamera.pixelWidth, mCamera.pixelHeight, 0, RenderTextureFormat.R8);
            computeShadowsCB.SetGlobalTexture("NGSS_Temporal_Tex", TempRT);
            computeShadowsCB.Blit(cShadow, cShadow2, mMaterial, 3);
            computeShadowsCB.Blit(cShadow2, TempRT);

            computeShadowsCB.SetGlobalTexture("NGSS_FrustumShadowsTexture", TempRT);//cShadow
        }
        else
        {
            computeShadowsCB.SetGlobalTexture("NGSS_FrustumShadowsTexture", cShadow);
        }

        isInitialized = true;
    }

    void OnEnable()
    {
        if (IsNotSupported())
        {
            Debug.LogWarning("Unsupported graphics API, NGSS requires at least SM3.0 or higher and DX9 is not supported.", this);
            this.enabled = false;
            return;
        }

        if (m_Temporal) { mCamera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors; } else { mCamera.depthTextureMode |= DepthTextureMode.Depth; }

        Init();
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat("NGSS_FRUSTUM_SHADOWS_ENABLED", 0f);

        if (isInitialized) { RemoveCommandBuffers(); }

        if (TempRT != null)
        {
            RenderTexture.ReleaseTemporary(TempRT);
            TempRT = null;
        }

        //mCamera.depthTextureMode &= ~(DepthTextureMode.MotionVectors);
        //#if UNITY_EDITOR
        //if (UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera != null)
        //UnityEditor.SceneView.currentDrawingSceneView.camera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        //#endif
    }

    void OnApplicationQuit()
    {
        if (isInitialized) { RemoveCommandBuffers(); }
    }

    void OnPreCull()
    {
        //Vector2 offset = GenerateRandomOffset();
        //mCamera.nonJitteredProjectionMatrix = mCamera.projectionMatrix;
        //mCamera.projectionMatrix = GetPerspectiveProjectionMatrix(offset);
        //mMaterial.SetMatrix("_TemporalMatrix", GetPerspectiveProjectionMatrix(offset));
    }

    void OnPreRender()
    {
        Init();
        if (isInitialized == false || mainShadowsLight == null) { return; }

        Shader.SetGlobalFloat("NGSS_FRUSTUM_SHADOWS_ENABLED", 1f);
        Shader.SetGlobalFloat("NGSS_FRUSTUM_SHADOWS_OPACITY", 1f - mainShadowsLight.shadowStrength);

        if (m_Temporal != isTemporal) { enabled = false; isTemporal = m_Temporal; enabled = true; }
        mMaterial.SetFloat("_TemporalScale", m_Temporal ? Mathf.Clamp(m_Scale, 0f, 0.99f) : 0f);
        mMaterial.SetVector("_Jitter_Offset", m_Temporal ? (GenerateRandomOffset() * m_Jittering) : Vector2.zero);

        //mMaterial.SetMatrix("InverseProj", Matrix4x4.Inverse(mCamera.projectionMatrix));//proj to cam        
        //mMaterial.SetMatrix("InverseView", mCamera.cameraToWorldMatrix);//cam to world        
        //mMaterial.SetMatrix("InverseViewProj", Matrix4x4.Inverse(GL.GetGPUProjectionMatrix(mCamera.projectionMatrix, false) * mCamera.worldToCameraMatrix));//proj to world        
        mMaterial.SetMatrix("WorldToView", mCamera.worldToCameraMatrix);//cam to world        
        mMaterial.SetVector("LightPos", mainShadowsLight.transform.position);//world position
        mMaterial.SetVector("LightDir", -mCamera.transform.InverseTransformDirection(mainShadowsLight.transform.forward));//view space direction
        mMaterial.SetVector("LightDirWorld", -mainShadowsLight.transform.forward);//view space direction
        //mMaterial.SetFloat("ShadowsOpacity", 1f - mainShadowsLight.shadowStrength);        
        mMaterial.SetFloat("ShadowsEdgeTolerance", m_shadowsEdgeBlur * 0.075f);
        mMaterial.SetFloat("ShadowsSoftness", m_shadowsBlur);
        //mMaterial.SetFloat("ShadowsDistance", m_shadowsDistance);
        mMaterial.SetFloat("RayScale", m_rayScale);
        //mMaterial.SetFloat("ShadowsFade", m_shadowsFade);
        mMaterial.SetFloat("ShadowsBias", m_shadowsBias * 0.02f);
        mMaterial.SetFloat("ShadowsDistanceStart", m_shadowsDistanceStart - 10f);
        mMaterial.SetFloat("RayThickness", m_rayThickness);
        mMaterial.SetFloat("RaySamples", (float)m_raySamples);
        //mMaterial.SetFloat("RaySamplesScale", m_raySamplesScale);
        if (m_deferredBackfaceOptimization && mCamera.actualRenderingPath == RenderingPath.DeferredShading) { mMaterial.EnableKeyword("NGSS_DEFERRED_OPTIMIZATION"); mMaterial.SetFloat("BackfaceOpacity", m_deferredBackfaceTranslucency); } else { mMaterial.DisableKeyword("NGSS_DEFERRED_OPTIMIZATION"); }
        if (m_dithering) { mMaterial.EnableKeyword("NGSS_USE_DITHERING"); } else { mMaterial.DisableKeyword("NGSS_USE_DITHERING"); }
        if (m_fastBlur) { mMaterial.EnableKeyword("NGSS_FAST_BLUR"); } else { mMaterial.DisableKeyword("NGSS_FAST_BLUR"); }
        if (mainShadowsLight.type != LightType.Directional) { mMaterial.EnableKeyword("NGSS_USE_LOCAL_SHADOWS"); } else { mMaterial.DisableKeyword("NGSS_USE_LOCAL_SHADOWS"); }
        //if (m_rayScreenScale) { mMaterial.EnableKeyword("NGSS_RAY_SCREEN_SCALE"); } else { mMaterial.DisableKeyword("NGSS_RAY_SCREEN_SCALE"); }
        mMaterial.SetFloat("RayScreenScale", m_rayScreenScale ? 1f : 0f);
    }
    //uncomment me if using the screen space blit | comment me if sampling directly from internal NGSS libraries
    void OnPostRender()
    {
        Shader.SetGlobalFloat("NGSS_FRUSTUM_SHADOWS_ENABLED", 0f);//don't render shadows
        //Shader.SetGlobalTexture("NGSS_FrustumShadowsTexture", noMixTexture);//don't render shadows
        //Shader.SetGlobalTexture("NGSS_Temporal_HistoryTex", noMixTexture);
        //mCamera.ResetProjectionMatrix();
    }

    private float GetHaltonValue(int index, int radix)
    {
        float result = 0.0f;
        float fraction = 1.0f / (float)radix;

        while (index > 0)
        {
            result += (float)(index % radix) * fraction;

            index /= radix;
            fraction /= (float)radix;
        }

        return result;
    }

    private Vector2 GenerateRandomOffset()
    {
        Vector2 offset = new Vector2(GetHaltonValue(m_SampleIndex & 1023, 2), GetHaltonValue(m_SampleIndex & 1023, 3));

        if (++m_SampleIndex >= 16)
            m_SampleIndex = 0;

        float vertical = Mathf.Tan(0.5f * Mathf.Deg2Rad * mCamera.fieldOfView);
        float horizontal = vertical * mCamera.aspect;

        offset.x *= horizontal / (0.5f * mCamera.pixelWidth);
        offset.y *= vertical / (0.5f * mCamera.pixelHeight);

        return offset;
    }
}
