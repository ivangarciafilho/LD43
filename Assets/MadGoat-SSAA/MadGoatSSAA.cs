using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace MadGoat_SSAA
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class MadGoatSSAA : MonoBehaviour
    {
        // Renderer
        public Mode renderMode = Mode.SSAA;
        public float multiplier = 1f;
        public float multiplierVertical = 1f;
        public bool fssaaAlpha;
        // SSAA 
        public SsaaProfile SSAA_X2 = new SsaaProfile(1.5f, true, Filter.BILINEAR, .8f, .5f);
        public SsaaProfile SSAA_X4 = new SsaaProfile(2f, true, Filter.BICUBIC, .725f, .95f);
        public SsaaProfile SSAA_HALF = new SsaaProfile(.5f, true, Filter.NEAREST_NEIGHBOR, 0, 0);
        public SSAAMode ssaaMode = SSAAMode.SSAA_OFF;
        public bool ssaaUltra=false;
        [Range(0, 1)]
        public float fssaaIntensity = 1;

        public RenderTextureFormat textureFormat = RenderTextureFormat.ARGBHalf;

        // Downsampler
        public bool useShader = true;
        public Filter filterType = Filter.BILINEAR;
        public float sharpness = 0.8f;
        public float sampleDistance = 1f;

        // Adaptive Resolution
        public bool useVsyncTarget = false;
        public int targetFramerate = 60;
        public float minMultiplier = 0.5f;
        public float maxMultiplier = 1.5f;

        // Screenshots
        public string screenshotPath = "Assets/SuperSampledSceenshots/";
        public string namePrefix = "SSAA";
        public bool useProductName = false;
        public ImageFormat imageFormat;
        [Range(0,100)]
        public int JPGQuality = 90;
        public bool EXR32 = false;

        // FSSAA
        private Shader _FXAA_FSS;
        protected Shader FXAA_FSS
        {
            get
            {
                if (_FXAA_FSS == null)
                    _FXAA_FSS = Shader.Find("Hidden/SSAA/FSS");

                return _FXAA_FSS;
            }
        }
        private Material _FXAA_FSS_Mat; // Default
        protected Material FXAA_FSS_Mat
        {
            get
            {
                if (_FXAA_FSS_Mat == null)
                    _FXAA_FSS_Mat = new Material(FXAA_FSS);

                return _FXAA_FSS_Mat;
            }
        }

        // Misc
        [SerializeField]
        protected Camera currentCamera;
        protected Camera renderCamera;
        protected GameObject renderCameraObject;
        protected MadGoatSSAA_InternalRenderer SSAA_Internal;
        private Rect tempRect;
        
        private Texture2D _sphereTemp;
        private Texture2D sphereTemp
        {
            get
            {
                if (_sphereTemp != null)
                    return _sphereTemp;
                _sphereTemp = new Texture2D(2, 2);
                return _sphereTemp;
            }
        }

        protected FramerateSampler FpsData = new FramerateSampler();
        public DebugData dbgData;

        // Misc settings
        public bool mouseCompatibilityMode;
        public RenderTexture targetTexture;
        public GameObject madGoatDebugger;
        // Screenshot Module
        public ScreenshotSettings screenshotSettings = new ScreenshotSettings();
        public PanoramaSettings panoramaSettings = new PanoramaSettings(1024,1);
     
        // ********* Private functions *********
        public virtual void Init()
        {
            if (renderCameraObject == null)
            {
                //Setup new high resolution camera
                renderCameraObject = new GameObject("RenderCameraObject");
                renderCameraObject.transform.SetParent(transform);
                renderCameraObject.transform.position = Vector3.zero;
                renderCameraObject.transform.rotation = new Quaternion(0, 0, 0, 0);
                renderCameraObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

                // Setup components of new camera
                renderCamera = renderCameraObject.AddComponent<Camera>();
                SSAA_Internal = renderCameraObject.AddComponent<MadGoatSSAA_InternalRenderer>();
                SSAA_Internal.current = renderCamera;
                SSAA_Internal.main = currentCamera;
                SSAA_Internal.enabled = true;

                // Copy settings from current camera
                renderCamera.CopyFrom(currentCamera);

                // Disable rendering on internal cam.
                // Nothing is drawn on main camera, performance hit is minimal
                renderCamera.cullingMask = 0;
                renderCamera.clearFlags = CameraClearFlags.Nothing;
            }
            else
                SSAA_Internal.enabled = true;

            currentCamera.targetTexture = new RenderTexture(1024, 1024, 24, textureFormat);
            currentCamera.targetTexture.Create();
        }

        // Unity stuff
        private void OnEnable()
        {
            if (dbgData == null)
                dbgData = new DebugData(this);

            currentCamera = GetComponent<Camera>();
            Init();
            StartCoroutine(AdaptiveTask());
        }
        private void Update()
        {
            currentCamera.targetTexture.filterMode = (filterType == Filter.NEAREST_NEIGHBOR && useShader) ? FilterMode.Point : FilterMode.Trilinear;

            renderCamera.enabled = currentCamera.enabled;
            renderCamera.CopyFrom(currentCamera, null);
          
            // Nothing is drawn on output camera, so the performance hit is minimal, we only need it to output the render (Graphics.Blit)
            renderCamera.cullingMask = mouseCompatibilityMode? -1 : 0;
            renderCamera.clearFlags = CameraClearFlags.Color;
            renderCamera.targetTexture = targetTexture;
            
            // Set render settings
            SSAA_Internal.multiplier = multiplier;
            SSAA_Internal.sharpness = sharpness;
            SSAA_Internal.useShader = useShader;
            SSAA_Internal.sampleDistance = sampleDistance;

            SSAA_Internal.ChangeMaterial(filterType);
            FpsData.Update();
            SendDbgInfo();
        }
        private void OnDisable()
        {
            SSAA_Internal.enabled = false;
            currentCamera.targetTexture.Release();
            currentCamera.targetTexture = null;
        }
        private void OnPreRender()
        {
            // Setup the aspect ratio
            currentCamera.aspect = (Screen.width*currentCamera.rect.width) / (Screen.height*currentCamera.rect.height) ;
            
            // If a screenshot is queued 
            if (screenshotSettings.takeScreenshot)
            {   // Setup for the screenshot and stop there.
                SetupScreenshotRender(screenshotSettings.screenshotMultiplier, false);
                return;
            }

            if (Screen.width * multiplier != currentCamera.targetTexture.width || Screen.height * (renderMode == Mode.PerAxisScale ? multiplierVertical : multiplier) != currentCamera.targetTexture.height)
            {
                SetupRender();
            }
            // Cache current camera rect and set it to fullscreen
            // Render Texture doesn't seem to like incomplete camera renders for some reason.
            tempRect = currentCamera.rect;
            currentCamera.rect = new Rect(0, 0, 1, 1);
        }
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (renderMode == Mode.SSAA && ssaaUltra && (ssaaMode == SSAAMode.SSAA_X2 || ssaaMode == SSAAMode.SSAA_X4))
                DoFSS(source, destination);
            else if (renderMode == Mode.ResolutionScale && ssaaUltra && multiplier > 1f)
                DoFSS(source, destination);
            else if (renderMode == Mode.Custom && ssaaUltra && multiplier > 1f)
                DoFSS(source, destination);
            else
                Graphics.Blit(source, destination);
        }
        private void OnPostRender()
        {
            // Reset the camera rect (to be used used by our internal camera in final output)
            currentCamera.rect = tempRect;
        }

        public void Refresh()
        {
            this.enabled = false;
            this.enabled = true;
            currentCamera.rect = new Rect(0, 0, 1, 1);
        }

        protected void SendDbgInfo()
        {
            if (!Application.isPlaying|| !madGoatDebugger)
                return;

            string message = "SSAA: Render Res:"+ GetResolution()+ " [x"+ dbgData.multiplier + "] [FSSAA:" +dbgData.fssaa+ "] [Mode: "+dbgData.renderMode+"]";
            madGoatDebugger.SendMessage("SsaaListener", message);
        }
        
        // Do The FSSAA 
        public virtual void DoFSS(RenderTexture source, RenderTexture destination)
        {
            // Preset for best quality
            FXAA_FSS_Mat.SetVector("_QualitySettings", new Vector3(1.0f, 0.063f, 0.0312f));
            FXAA_FSS_Mat.SetVector("_ConsoleSettings", new Vector4(0.5f, 2.0f, 0.125f, 0.04f));
            FXAA_FSS_Mat.SetFloat("_Intensity", fssaaIntensity);
            Graphics.Blit(source, destination, FXAA_FSS_Mat, 0);
        }
        // Renders a 360 panorama image and save to disk
        private void RenderPanorama()
        {
            // init
            enabled = false;
            int internalRes = panoramaSettings.panoramaSize * panoramaSettings.panoramaMultiplier;
            Cubemap resultCube = new Cubemap(internalRes, TextureFormat.ARGB32, false);
           
            RenderTexture buffer = RenderTexture.GetTemporary(panoramaSettings.panoramaSize, panoramaSettings.panoramaSize,24,RenderTextureFormat.ARGB32);

            // reset the render camera
            renderCamera.CopyFrom(currentCamera, null);
            SSAA_Internal.enabled = false;
            currentCamera.RenderToCubemap(resultCube);

            string folderPath = screenshotPath + "\\" + getName + "\\";
            (new FileInfo(folderPath)).Directory.Create();

            for (int i = 0; i < 6; i++)
            {
                sphereTemp.Resize(internalRes, internalRes);
                sphereTemp.SetPixels(Rotate90(Rotate90(resultCube.GetPixels((CubemapFace)i), internalRes),internalRes));
                sphereTemp.Apply();
               
                // no processing needs to be done if no supersampling
                if (panoramaSettings.panoramaMultiplier == 1)
                {

                    if (imageFormat == ImageFormat.PNG)
                        File.WriteAllBytes(folderPath+"Face_" + (CubemapFace)i + ".png", sphereTemp.EncodeToPNG());
                    else if (imageFormat == ImageFormat.JPG)
                        File.WriteAllBytes(folderPath + "Face_" + (CubemapFace)i + ".jpg", sphereTemp.EncodeToJPG(JPGQuality));
#if UNITY_5_6_OR_NEWER
                    else
                        File.WriteAllBytes(folderPath + "Face_" + (CubemapFace)i + ".exr", sphereTemp.EncodeToEXR(EXR32 ? Texture2D.EXRFlags.OutputAsFloat : Texture2D.EXRFlags.None));
#endif
                    continue;
                }

                bool sRGBWrite = GL.sRGBWrite;
                // enable srgb conversion for blit - fixes the color issue
                GL.sRGBWrite = true;

                if (!panoramaSettings.useFilter)
                {
                    Graphics.Blit(sphereTemp, buffer);
                }
                else
                {
                    SSAA_Internal.bicubicMaterial.SetFloat("_ResizeWidth", internalRes);
                    SSAA_Internal.bicubicMaterial.SetFloat("_ResizeHeight", internalRes);

                    SSAA_Internal.bicubicMaterial.SetFloat("_Sharpness", panoramaSettings.sharpness);
                    Graphics.Blit(sphereTemp, buffer, SSAA_Internal.bicubicMaterial, 0);
                }
                RenderTexture.active = buffer;
                
                Texture2D screenshotBuffer = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.ARGB32, true, true);
                screenshotBuffer.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);

                if (imageFormat == ImageFormat.PNG)
                    File.WriteAllBytes(folderPath + "\\Face_" + (CubemapFace)i + ".png", screenshotBuffer.EncodeToPNG());
                else if (imageFormat == ImageFormat.JPG)
                    File.WriteAllBytes(folderPath + "\\Face_" + (CubemapFace)i + ".jpg", screenshotBuffer.EncodeToJPG(JPGQuality));
#if UNITY_5_6_OR_NEWER
                else
                    File.WriteAllBytes(folderPath + "\\Face_" + (CubemapFace)i + ".exr", screenshotBuffer.EncodeToEXR(EXR32 ? Texture2D.EXRFlags.OutputAsFloat : Texture2D.EXRFlags.None));
#endif
                // restore the sRGBWrite to older state so it doesn't interfere with user's setting
                GL.sRGBWrite = sRGBWrite;
            }

            // Clean some allocated memory
            sphereTemp.Resize(2, 2);
            sphereTemp.Apply();
            RenderTexture.ReleaseTemporary(buffer);
            // SSAA can render again
            SSAA_Internal.enabled = true;
            enabled = true;
        }
        // Setup the multiplier in adaptive mode
        private void SetupAdaptive(int fps)
        {
            int compFramerate = useVsyncTarget ? Screen.currentResolution.refreshRate : targetFramerate;
            if (fps < compFramerate - 5)
            {
                multiplier = Mathf.Clamp(multiplier - 0.1f, minMultiplier, maxMultiplier);
            }
            else if(fps> compFramerate + 10)
            {
                multiplier = Mathf.Clamp(multiplier + 0.1f, minMultiplier, maxMultiplier);
            }
        }
        // Setup for SSAA renderer
        private void SetupRender()
        {
            try
            {
                currentCamera.targetTexture.Release();
                currentCamera.targetTexture.width = (int)(Screen.width * multiplier);
                currentCamera.targetTexture.height = (int)(Screen.height * (renderMode == Mode.PerAxisScale ? multiplierVertical : multiplier));
                currentCamera.targetTexture.Create();
            }
            catch (Exception ex)
            {
                Debug.LogError("Something went wrong. SSAA has been set to off");
                Debug.LogError(ex);
                SetAsSSAA(SSAAMode.SSAA_OFF);
            }
        }
        // Setup for ScreenShot Render
        private void SetupScreenshotRender(float mul, bool compatibilityMode)
        {
            try
            {
                // If taking a screenshot, the aspect ratio should be given by the screenshot resolution, not the screenres.
                currentCamera.aspect = screenshotSettings.outputResolution.x / screenshotSettings.outputResolution.y;

                currentCamera.targetTexture.Release();
                currentCamera.targetTexture.width = (int)(screenshotSettings.outputResolution.x * mul);
                currentCamera.targetTexture.height = (int)(screenshotSettings.outputResolution.y * mul);
                currentCamera.targetTexture.Create();
            }
            catch (Exception ex) { Debug.LogError(ex.ToString()); }
        }
        // The adaptive mode coroutine
        protected IEnumerator AdaptiveTask()
        {
            yield return new WaitForSeconds(2);
            if(renderMode == Mode.AdaptiveResolution)
                SetupAdaptive(FpsData.CurrentFps);

            if(enabled)
                StartCoroutine(AdaptiveTask());
        }
        

        // Used to rotate the 360 panorama images
        private Color[] Rotate90(Color[] source, int n)
        {
            Color[] result = new Color[n * n];

            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    result[i * n + j] = source[(n - j - 1) * n + i];
                }
            }
            return result;
        }
        private string getName // generate a string for the filename of the screenshot
        {
            get
            {
                return (useProductName ? Application.productName : namePrefix )+ "_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmssff")+ "_" +
                    panoramaSettings.panoramaSize.ToString() + "p";
            }
        }
        /**************************************************************************************************
         *                                            PUBLIC API                                          *
         **************************************************************************************************/
        /// <summary>
        /// Set rendering mode to given SSAA mode
        /// </summary>
        public void SetAsSSAA(SSAAMode mode)
        {
            renderMode = Mode.SSAA;
            ssaaMode = mode;
            switch (mode)
            {
                case SSAAMode.SSAA_OFF:
                    multiplier = 1f;
                    useShader = false;
                    break;
                case SSAAMode.SSAA_HALF:
                    multiplier = SSAA_HALF.multiplier;
                    useShader = SSAA_HALF.useFilter;
                    sharpness = SSAA_HALF.sharpness;
                    filterType = SSAA_HALF.filterType;
                    sampleDistance = SSAA_HALF.sampleDistance;
                    break;
                case SSAAMode.SSAA_X2:
                    multiplier = SSAA_X2.multiplier;
                    useShader = SSAA_X2.useFilter;
                    sharpness = SSAA_X2.sharpness;
                    filterType = SSAA_X2.filterType;
                    sampleDistance = SSAA_X2.sampleDistance;
                    break;
                case SSAAMode.SSAA_X4:
                    multiplier = SSAA_X4.multiplier;
                    useShader = SSAA_X4.useFilter;
                    sharpness = SSAA_X4.sharpness;
                    filterType = SSAA_X4.filterType;
                    sampleDistance = SSAA_X4.sampleDistance;
                    break;
            }
        }

        /// <summary>
        /// Set the resolution scale to a given percent
        /// </summary>
        public void SetAsScale(int percent)
        {
            // check for invalid values
            percent = Mathf.Clamp(percent, 0, 100);

            renderMode = Mode.ResolutionScale;
            multiplier = percent / 100f;

            SetDownsamplingSettings(false);
        }
        /// <summary>
        /// Set the resolution scale to a given percent, and use custom downsampler settings
        /// </summary>
        public void SetAsScale(int percent, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            // check for invalid values
            percent = Mathf.Clamp(percent, 0, 100);

            renderMode = Mode.ResolutionScale;
            multiplier = percent / 100f;

            SetDownsamplingSettings(FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the operation mode as adaptive with target frame rate
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="targetFramerate"></param>
        public void SetAsAdaptive(float minMultiplier, float maxMultiplier, int targetFramerate)
        {
            // check for invalid values
            if (minMultiplier < 0.1f) minMultiplier = 0.1f;
            if (maxMultiplier < minMultiplier) maxMultiplier = minMultiplier + 0.1f;

            this.minMultiplier = minMultiplier;
            this.maxMultiplier = maxMultiplier;
            this.targetFramerate = targetFramerate;
            useVsyncTarget = false;
            SetDownsamplingSettings(false);
        }
        /// <summary>
        /// Set the operation mode as adaptive with screen refresh rate as target frame rate
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        public void SetAsAdaptive(float minMultiplier, float maxMultiplier)
        {
            // check for invalid values
            if (minMultiplier < 0.1f) minMultiplier = 0.1f;
            if (maxMultiplier < minMultiplier) maxMultiplier = minMultiplier + 0.1f;

            this.minMultiplier = minMultiplier;
            this.maxMultiplier = maxMultiplier;
            useVsyncTarget = true;
            SetDownsamplingSettings(false);
        }
        /// <summary>
        /// Set the operation mode as adaptive with target frame rate and use downsampling filter.
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="targetFramerate"></param>
        /// <param name="FilterType"></param>
        /// <param name="sharpnessfactor"></param>
        /// <param name="sampledist"></param>
        public void SetAsAdaptive(float minMultiplier, float maxMultiplier, int targetFramerate, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            // check for invalid values
            if (minMultiplier < 0.1f) minMultiplier = 0.1f;
            if (maxMultiplier < minMultiplier) maxMultiplier = minMultiplier + 0.1f;

            this.minMultiplier = minMultiplier;
            this.maxMultiplier = maxMultiplier;
            this.targetFramerate = targetFramerate;
            useVsyncTarget = false;

            SetDownsamplingSettings(FilterType, sharpnessfactor, sampledist);
        }
        /// <summary>
        /// Set the operation mode as adaptive with screen refresh rate as target frame rate and use downsampling filter.
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="FilterType"></param>
        /// <param name="sharpnessfactor"></param>
        /// <param name="sampledist"></param>
        public void SetAsAdaptive(float minMultiplier, float maxMultiplier, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            // check for invalid values
            if (minMultiplier < 0.1f) minMultiplier = 0.1f;
            if (maxMultiplier < minMultiplier) maxMultiplier = minMultiplier + 0.1f;

            this.minMultiplier = minMultiplier;
            this.maxMultiplier = maxMultiplier;
            useVsyncTarget = true;

            SetDownsamplingSettings(FilterType, sharpnessfactor, sampledist);
        }
        
        /// <summary>
        /// Set a custom resolution multiplier
        /// </summary>
        public void SetAsCustom(float Multiplier)
        {
            // check for invalid values
            if (Multiplier < 0.1f) Multiplier = 0.1f;

            renderMode = Mode.Custom;
            multiplier = Multiplier;

            SetDownsamplingSettings(false);
        }
        /// <summary>
        /// Set a custom resolution multiplier, and use custom downsampler settings
        /// </summary>
        public void SetAsCustom(float Multiplier, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            // check for invalid values
            if (Multiplier < 0.1f) Multiplier = 0.1f;

            renderMode = Mode.Custom;
            multiplier = Multiplier;

            SetDownsamplingSettings(FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the multiplier of each screen axis independently. does not use downsampling filter.
        /// </summary>
        /// <param name="MultiplierX"></param>
        /// <param name="MultiplierY"></param>
        public virtual void SetAsAxisBased(float MultiplierX, float MultiplierY)
        {
            // check for invalid values
            if (MultiplierX < 0.1f) MultiplierX = 0.1f;
            if (MultiplierY < 0.1f) MultiplierY = 0.1f;

            renderMode = Mode.PerAxisScale;
            multiplier = MultiplierX;
            multiplierVertical = MultiplierY;

            SetDownsamplingSettings(false);
        }
        /// <summary>
        /// Set the multiplier of each screen axis independently while using the downsampling filter.
        /// </summary>
        /// <param name="MultiplierX"></param>
        /// <param name="MultiplierY"></param>
        public virtual void SetAsAxisBased(float MultiplierX, float MultiplierY, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            // check for invalid values
            if (MultiplierX < 0.1f) MultiplierX = 0.1f;
            if (MultiplierY < 0.1f) MultiplierY = 0.1f;

            renderMode = Mode.PerAxisScale;
            multiplier = MultiplierX;
            multiplierVertical = MultiplierY;

            SetDownsamplingSettings(FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the downsampling shader parameters. If the case, this should be called after setting the mode, otherwise it might get overrided. (ex: SSAA)
        /// </summary>
        public void SetDownsamplingSettings(bool use)
        {
            useShader = use;
            filterType = use ? Filter.BILINEAR : Filter.NEAREST_NEIGHBOR;
            sharpness = use ? 0.85f : 0; // 0.85 should work fine for any resolution 
            sampleDistance = use ? 0.9f : 0; // 0.9 should work fine for any res
        }
        /// <summary>
        /// Set the downsampling shader parameters. If the case, this should be called after setting the mode, otherwise it might get overrided. (ex: SSAA)
        /// </summary>
        public void SetDownsamplingSettings(Filter FilterType, float sharpnessfactor, float sampledist)
        {
            useShader = true;
            filterType = FilterType;
            sharpness = Mathf.Clamp(sharpnessfactor, 0, 1);
            sampleDistance = Mathf.Clamp(sampledist, 0.5f, 1.5f);
        }

        /// <summary>
        /// Enable or disable the ultra mode for super sampling.(FSS)
        /// </summary>
        /// <param name="enabled"></param>
        public void SetUltra(bool enabled)
        {
            ssaaUltra = enabled;
        }
        /// <summary>
        /// Set the intensity of the SSAA ultra effect (FSSAA intensity)
        /// </summary>
        /// <param name="intensity"></param>
        public void SetUltraIntensity(float intensity)
        {
            fssaaIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Take a screenshot of resolution Size (x is width, y is height) rendered at a higher resolution given by the multiplier. The screenshot is saved at the given path in PNG format.
        /// </summary>
        public virtual void TakeScreenshot(string path, Vector2 Size, int multiplier)
        {
            // Take screenshot with default settings
            screenshotSettings.takeScreenshot = true;
            screenshotSettings.outputResolution = Size;
            screenshotSettings.screenshotMultiplier = multiplier;
            screenshotPath = path;
            
            screenshotSettings.useFilter = false;
        }
        /// <summary>
        /// Take a screenshot of resolution Size (x is width, y is height) rendered at a higher resolution given by the multiplier and use the bicubic downsampler. The screenshot is saved at the given path in PNG format. 
        /// </summary>
        public virtual void TakeScreenshot(string path, Vector2 Size, int multiplier, float sharpness)
        {
            // Take screenshot with custom settings
            screenshotSettings.takeScreenshot = true;
            screenshotSettings.outputResolution = Size;
            screenshotSettings.screenshotMultiplier = multiplier;
            screenshotPath = path;
            screenshotSettings.useFilter = true;
            screenshotSettings.sharpness = Mathf.Clamp(sharpness, 0, 1);
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public virtual void TakePanorama(string path, int size)
        {
            panoramaSettings.useFilter = false;
            panoramaSettings.panoramaSize = size;
            panoramaSettings.panoramaMultiplier = 1;

            screenshotPath = path;
            RenderPanorama();
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" supersampled by "multiplier"
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public virtual void TakePanorama(string path, int size, int multiplier)
        {
            panoramaSettings.useFilter = false;
            panoramaSettings.panoramaSize = size;
            panoramaSettings.panoramaMultiplier = multiplier;
            
            screenshotPath = path;
            RenderPanorama();
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" using downsampling shader
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public virtual void TakePanorama(string path, int size, int multiplier, float sharpness)
        {
            panoramaSettings.useFilter = true;
            panoramaSettings.panoramaSize = size;
            panoramaSettings.panoramaMultiplier = multiplier;
            panoramaSettings.sharpness = sharpness;

            screenshotPath = path;
            RenderPanorama();
        }

        /// <summary>
        /// Sets up the screenshot module to use the PNG image format. This enables transparency in output images.
        /// </summary>
        public virtual void SetScreenshotModuleToPNG()
        {
            this.imageFormat = ImageFormat.PNG;
        }
        /// <summary>
        /// Sets up the screenshot module to use the JPG image format. Quality is parameter from 1 to 100 and represents the compression quality of the JPG file. Incorrect quality values will be clamped.
        /// </summary>
        /// <param name="quality"></param>
        public virtual void SetScreenshotModuleToJPG(int quality)
        {
            this.imageFormat = ImageFormat.JPG;
            this.JPGQuality = Mathf.Clamp(1,100,quality);
        }
#if UNITY_5_6_OR_NEWER
        /// <summary>
        /// Sets up the screenshot module to use the EXR image format. The EXR32 bool parameter dictates whether to use or not 32 bit exr encoding. This method is only available in Unity 5.6 and newer.
        /// </summary>
        /// <param name="EXR32"></param>
        public virtual void SetScreenshotModuleToEXR(bool EXR32)
        {
            this.imageFormat = ImageFormat.EXR;
            this.EXR32 = EXR32;
        }
#endif

        /// <summary>
        /// Return string with current internal resolution
        /// </summary>
        /// <returns></returns>
        public virtual string GetResolution()
        {
            return (int)(Screen.width * multiplier) + "x" + (int)(Screen.height * multiplier);
        }

        // Global api
        /// <summary>
        /// Set rendering mode to given SSAA mode
        /// </summary>
        public static void SetAllAsSSAA(SSAAMode mode)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsSSAA(mode);
        }

        /// <summary>
        /// Set the resolution scale to a given percent
        /// </summary>
        public static void SetAllAsScale(int percent)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsScale(percent);
        }
        /// <summary>
        /// Set the resolution scale to a given percent, and use custom downsampler settings
        /// </summary>
        public static void SetAllAsScale(int percent, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsScale(percent, FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the operation mode as adaptive with target frame rate
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="targetFramerate"></param>
        public static void SetAllAsAdaptive(float minMultiplier, float maxMultiplier, int targetFramerate)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAdaptive(minMultiplier, maxMultiplier, targetFramerate);
        }
        /// <summary>
        /// Set the operation mode as adaptive with screen refresh rate as target frame rate
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        public static void SetAllAsAdaptive(float minMultiplier, float maxMultiplier)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAdaptive(minMultiplier, maxMultiplier);
        }
        /// <summary>
        /// Set the operation mode as adaptive with target frame rate and use downsampling filter.
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="targetFramerate"></param>
        /// <param name="FilterType"></param>
        /// <param name="sharpnessfactor"></param>
        /// <param name="sampledist"></param>
        public static void SetAllAsAdaptive(float minMultiplier, float maxMultiplier, int targetFramerate, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAdaptive(minMultiplier, maxMultiplier, targetFramerate, FilterType, sharpnessfactor, sampledist);
        }
        /// <summary>
        /// Set the operation mode as adaptive with screen refresh rate as target frame rate and use downsampling filter.
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="FilterType"></param>
        /// <param name="sharpnessfactor"></param>
        /// <param name="sampledist"></param>
        public static void SetAllAsAdaptive(float minMultiplier, float maxMultiplier, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAdaptive(minMultiplier, maxMultiplier, FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set a custom resolution multiplier
        /// </summary>
        public static void SetAllAsCustom(float Multiplier)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsCustom(Multiplier);
        }
        /// <summary>
        /// Set a custom resolution multiplier, and use custom downsampler settings
        /// </summary>
        public static void SetAllAsCustom(float Multiplier, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsCustom(Multiplier, FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the multiplier of each screen axis independently. does not use downsampling filter.
        /// </summary>
        /// <param name="MultiplierX"></param>
        /// <param name="MultiplierY"></param>
        public static void SetAllAsAxisBased(float MultiplierX, float MultiplierY)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAxisBased(MultiplierX, MultiplierY);
        }
        /// <summary>
        ///  Set the multiplier of each screen axis independently while using the downsampling filter.
        /// </summary>
        public static void SetAllAsAxisBased(float MultiplierX, float MultiplierY, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAxisBased(MultiplierX, MultiplierY, FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the downsampling shader parameters. If the case, this should be called after setting the mode, otherwise it might get overrided. (ex: SSAA)
        /// </summary>
        public static void SetAllDownsamplingSettings(bool use)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetDownsamplingSettings(use);
        }
        /// <summary>
        /// Set the downsampling shader parameters. If the case, this should be called after setting the mode, otherwise it might get overrided. (ex: SSAA)
        /// </summary>
        public static void SetAllDownsamplingSettings(Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetDownsamplingSettings(FilterType,sharpnessfactor,sampledist);
        }

        /// <summary>
        /// Enable or disable the ultra mode for super sampling.(FSS)
        /// </summary>
        /// <param name="enabled"></param>
        public static void SetAllUltra(bool enabled)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetUltra(enabled);
        }
        /// <summary>
        /// Set the intensity of the SSAA ultra effect (FSSAA intensity)
        /// </summary>
        /// <param name="enabled"></param>
        public static void SetAllUltraIntensity(float intensity)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetUltraIntensity(intensity);
        }

        /// <summary>
        /// Returns a ray from a given screenpoint
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Ray ScreenPointToRay(Vector3 position)
        {
            return renderCamera.ScreenPointToRay(position);
        }
        /// <summary>
        /// Transforms postion from screen space into viewport space.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector3 ScreenToViewportPoint(Vector3 position)
        {
            return renderCamera.ScreenToViewportPoint(position);
        }
        /// <summary>
        /// Transforms position from screen space into world space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector3 ScreenToWorldPoint(Vector3 position)
        {
            return renderCamera.ScreenToWorldPoint(position);
        }
        /// <summary>
        /// Transforms position from world space to screen space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector3 WorldToScreenPoint(Vector3 position)
        {
            return renderCamera.WorldToScreenPoint(position);
        }
        /// <summary>
        /// Transforms position from viewport space to screen space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector3 ViewportToScreenPoint(Vector3 position)
        {
            return renderCamera.ViewportToScreenPoint(position);
        }
    }
}
 