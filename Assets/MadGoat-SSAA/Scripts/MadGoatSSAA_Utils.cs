using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
namespace MadGoat_SSAA
{
    public enum Mode
    {
        SSAA,
        ResolutionScale,
        PerAxisScale,
        AdaptiveResolution,
        Custom
    }
    public enum SSAAMode
    {
        SSAA_OFF = 0,
        SSAA_HALF = 1,
        SSAA_X2 = 2,
        SSAA_X4 = 3
    }
    public enum Filter
    {
        NEAREST_NEIGHBOR,
        BILINEAR,
        BICUBIC
    }
    public enum ImageFormat
    {
        JPG,
        PNG,
        #if UNITY_5_6_OR_NEWER
        EXR
        #endif
    }
    public enum EditorPanoramaRes
    {
        Square128 = 128,
        Square256 = 256,
        Square512 = 512,
        Square1024 = 1024,
        Square2048 = 2048,
        Square4096 = 4096,

    }
    [System.Serializable]
    public class SsaaProfile
    {
        [HideInInspector]
        public float multiplier;

        public bool useFilter;
        [Tooltip("Which type of filtering to be used (only applied if useShader is true)")]
        public Filter filterType = Filter.BILINEAR;
        [Tooltip("The sharpness of the filtered image (only applied if useShader is true)")]
        [Range(0, 1)]
        public float sharpness;
        [Tooltip("The distance between the samples (only applied if useShader is true)")]
        [Range(0.5f, 2f)]
        public float sampleDistance;

        public SsaaProfile(float mul, bool useDownsampling)
        {
            multiplier = mul;

            useFilter = useDownsampling;
            sharpness = useDownsampling ? 0.85f : 0;
            sampleDistance = useDownsampling ? 0.65f : 0;
        }
        public SsaaProfile(float mul, bool useDownsampling, Filter filterType, float sharp, float sampleDist)
        {
            multiplier = mul;

            this.filterType = filterType;
            useFilter = useDownsampling;
            sharpness = useDownsampling ? sharp : 0;
            sampleDistance = useDownsampling ? sampleDist : 0;
        }
    }
    [System.Serializable]
    public class ScreenshotSettings
    {
        [HideInInspector]
        public bool takeScreenshot = false;

        [Range(1, 4)]
        public int screenshotMultiplier = 1;
        public Vector2 outputResolution = new Vector2(1920, 1080);

        public bool useFilter = true;
        [Range(0, 1)]
        public float sharpness = 0.85f;
    }
    [System.Serializable]
    public class PanoramaSettings
    {
        public PanoramaSettings(int size, int mul)
        {
            panoramaMultiplier = mul;
            panoramaSize = size;
        }
        public int panoramaSize;

        [Range(1,4)]
        public int panoramaMultiplier;

        public bool useFilter = true;
        [Range(0, 1)]
        public float sharpness = 0.85f;
    }
    public class DebugData
    {
        public MadGoatSSAA instance;

        public Mode renderMode
        {
            get { return instance.renderMode; }
        }
        public float multiplier
        {
            get { return instance.multiplier; }
        }
        public bool fssaa
        {
            get { return instance.ssaaUltra; }
        }

        // Constructor
        public DebugData(MadGoatSSAA instance)
        {
            this.instance = instance;
        }
    }
    public static class MadGoatSSAA_Utils
    {
        public const string ssaa_version = "1.6.1"; // Don't forget to change me when pushing updates!

        /// <summary>
        /// Makes this camera's settings match the other camera and assigns a custom target texture
        /// </summary>
        public static void CopyFrom(this Camera current, Camera other, RenderTexture rt)
        {
            current.CopyFrom(other);
            current.targetTexture = rt;
        }

    }
}