using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MadGoat_SSAA;
using System;
namespace MadGoat_SSAA
{
    public class MadGoatSSAA_VR : MadGoatSSAA
    {
        // Shader Setup
        [SerializeField]
        private Shader _bilinearshader;
        public Shader bilinearshader
        {
            get
            {
                if (_bilinearshader == null)
                    _bilinearshader = Shader.Find("Hidden/SSAA_Bilinear");

                return _bilinearshader;
            }
        }
        [SerializeField]
        private Shader _bicubicshader;
        public Shader bicubicshader
        {
            get
            {
                if (_bicubicshader == null)
                    _bicubicshader = Shader.Find("Hidden/SSAA_Bicubic");

                return _bicubicshader;
            }
        }
        [SerializeField]
        private Shader _neighborshader;
        public Shader neighborshader
        {
            get
            {
                if (_neighborshader == null)
                {
                    _neighborshader = Shader.Find("Hidden/SSAA_Nearest");
                }
                return _neighborshader;
            }
        }

        private Material material_bl; // Bilinear Material
        private Material material_bc; // Bicubic
        private Material material_nn; // Nearest Neighbor

        private Material material_current;

        private void OnEnable()
        {
            if (dbgData == null)
                dbgData = new DebugData(this);

            Init();
            StartCoroutine(AdaptiveTask());
        }
        private void Update()
        {
            FpsData.Update();
            SendDbgInfo();
        }
        private void OnDisable()
        {
#if UNITY_2017_2_OR_NEWER
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1f;
#else
            UnityEngine.VR.VRSettings.renderScale = 1f;
#endif
        }
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            RenderTexture buff = RenderTexture.GetTemporary(source.width, source.height, 24, source.format);

            // fix for singlepass issue in u2018
            #if UNITY_2017_2_OR_NEWER
                    buff.vrUsage = VRTextureUsage.TwoEyes;
            #endif
            

            if (renderMode == Mode.SSAA && ssaaUltra && (ssaaMode == SSAAMode.SSAA_X2 || ssaaMode == SSAAMode.SSAA_X4))
                DoFSS(source, buff);
            else if (renderMode == Mode.ResolutionScale && ssaaUltra && multiplier > 1f)
                DoFSS(source, buff);
            else if (renderMode == Mode.Custom && ssaaUltra && multiplier > 1f)
                DoFSS(source, buff);
            else
                Graphics.Blit(source, buff);

            // Effect is disabled or we don't use custom downsampling
            if (!useShader || multiplier == 1f)
            {
                Graphics.Blit(buff, destination);
            }
            else // Setup the custom downsampler and output
            {
                material_current.SetFloat("_ResizeWidth", Screen.width);
                material_current.SetFloat("_ResizeHeight", Screen.height);
                material_current.SetFloat("_Sharpness", sharpness);
                material_current.SetFloat("_SampleDistance", sampleDistance);
                Graphics.Blit(source, destination, material_current, 0);
            }
            buff.Release();
            buff = null;
        }
        private void OnPostRender()
        {
            // just to override base.
        }
        private void OnPreRender()
        {
            try
            {
#if UNITY_2017_2_OR_NEWER
            if (!UnityEngine.XR.XRDevice.isPresent)
                throw new Exception("VRDevice not present or not detected");
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = multiplier;
#else
                if (!UnityEngine.VR.VRDevice.isPresent)
                    throw new Exception("VRDevice not present or not detected");
                UnityEngine.VR.VRSettings.renderScale = multiplier;
#endif

                ChangeMaterial(filterType);
            }
            catch (Exception ex)
            {
                Debug.LogError("Something went wrong. SSAA has been set to off and plugin was disabled");
                Debug.LogError(ex);
                SetAsSSAA(SSAAMode.SSAA_OFF);
                enabled = false;
            }
        }

        private void ChangeMaterial(Filter Type)
        {
            // Point material_current to the given material
            switch (Type)
            {
                case Filter.NEAREST_NEIGHBOR:
                    material_current = material_nn;
                    break;
                case Filter.BILINEAR:
                    material_current = material_bl;
                    break;
                case Filter.BICUBIC:
                    material_current = material_bc;
                    break;
            }
        }
        public override void Init()
        {
            if (currentCamera == null)
                currentCamera = GetComponent<Camera>();

#if UNITY_2017_2_OR_NEWER
        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = multiplier;
#else
            UnityEngine.VR.VRSettings.renderScale = multiplier;
#endif

            if (material_bl == null)
                material_bl = new Material(bilinearshader);
            if (material_bc == null)
                material_bc = new Material(bicubicshader);
            if (material_nn == null)
                material_nn = new Material(neighborshader);

            material_current = material_bc;
        }

        /// <summary>
        /// Set the multiplier of each screen axis independently. does not use downsampling filter.
        /// </summary>
        public override void SetAsAxisBased(float MultiplierX, float MultiplierY)
        {
            Debug.LogWarning("NOT SUPPORTED IN VR MODE.\nX axis will be used as global multiplier instead.");
            base.SetAsAxisBased(MultiplierX, MultiplierY);
        }
        /// <summary>
        ///  Set the multiplier of each screen axis independently while using the downsampling filter.
        /// </summary>
        public override void SetAsAxisBased(float MultiplierX, float MultiplierY, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            Debug.LogWarning("NOT SUPPORTED IN VR MODE.\nX axis will be used as global multiplier instead.");
            base.SetAsAxisBased(MultiplierX, MultiplierY, FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Returns a ray from a given screenpoint
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Ray ScreenPointToRay(Vector3 position)
        {
            return currentCamera.ScreenPointToRay(position);
        }
        /// <summary>
        /// Transforms position from screen space into world space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 ScreenToWorldPoint(Vector3 position)
        {
            return currentCamera.ScreenToWorldPoint(position);
        }
        /// <summary>
        /// Transforms postion from screen space into viewport space.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 ScreenToViewportPoint(Vector3 position)
        {
            return currentCamera.ScreenToViewportPoint(position);
        }
        /// <summary>
        /// Transforms position from world space to screen space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 WorldToScreenPoint(Vector3 position)
        {
            return currentCamera.WorldToScreenPoint(position);
        }
        /// <summary>
        /// Transforms position from viewport space to screen space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 ViewportToScreenPoint(Vector3 position)
        {
            return currentCamera.ViewportToScreenPoint(position);
        }


        /// <summary>
        /// Take a screenshot of resolution Size (x is width, y is height) rendered at a higher resolution given by the multiplier. The screenshot is saved at the given path in PNG format.
        /// </summary>
        public override void TakeScreenshot(string path, Vector2 Size, int multiplier)
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Take a screenshot of resolution Size (x is width, y is height) rendered at a higher resolution given by the multiplier and use the bicubic downsampler. The screenshot is saved at the given path in PNG format. 
        /// </summary>
        public override void TakeScreenshot(string path, Vector2 Size, int multiplier, float sharpness)
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public override void TakePanorama(string path, int size)
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" supersampled by "multiplier"
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public override void TakePanorama(string path, int size, int multiplier)
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" using downsampling shader
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public override void TakePanorama(string path, int size, int multiplier, float sharpness)
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Sets up the screenshot module to use the PNG image format. This enables transparency in output images.
        /// </summary>
        public override void SetScreenshotModuleToPNG()
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Sets up the screenshot module to use the JPG image format. Quality is parameter from 1 to 100 and represents the compression quality of the JPG file. Incorrect quality values will be clamped.
        /// </summary>
        /// <param name="quality"></param>
        public override void SetScreenshotModuleToJPG(int quality)
        {
            Debug.LogWarning("Not available in VR mode");
        }
#if UNITY_5_6_OR_NEWER
        /// <summary>
        /// Sets up the screenshot module to use the EXR image format. The EXR32 bool parameter dictates whether to use or not 32 bit exr encoding. This method is only available in Unity 5.6 and newer.
        /// </summary>
        /// <param name="EXR32"></param>
        public override void SetScreenshotModuleToEXR(bool EXR32)
        {
            Debug.LogWarning("Not available in VR mode");
        }
#endif
    }
}