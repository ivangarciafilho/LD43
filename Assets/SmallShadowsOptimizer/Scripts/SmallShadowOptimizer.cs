using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using System;

public class SmallShadowOptimizer : MonoBehaviour {

    const string DLLName =
#if UNITY_IPHONE || UNITY_XBOX360
   
               // On iOS and Xbox 360 plugins are statically linked into
               // the executable, so we have to use __Internal as the
               // library name.
               "__Internal"

#else

            // Other platforms load plugins dynamically, so pass the name
            // of the plugin's dynamic library.
            "REPlugin"

#endif
        ;

    //C++ method that sets up a frustum from the Unity Camera, must be called prior to CheckBounds and is used in CheckBounds
    [DllImport(DLLName)]
    public static extern void SetFrustum(float fAspectRatio, float VerticalFOV, float Near, float Far, Vector3 Position, Vector3 View, Vector3 UpVector);

    //C++ method that checks visibility, updates visibility array "results" and computes visibility deltas in resultsDiff
    //Note : Shadow visibility is calculated only for frustum visible objects in order to reduce the loop iterations in C#
    //objects not currently visible will have their castShadows parameter untouched
    [DllImport(DLLName)]    
    public static extern void CheckBounds(float[] boundsArray, int numBounds, int[] results, float SizeThreshold, int[] resultsDiff, ref int diffSize);

    // Threshold at which shadows will not be cast anymore; this is a combination of distance and object size
    //Adjust this value to reduce more/less small object shadows
    public float SizeThreshold = 16;

    //Camera used for frustum culling; if it's null, it is assumed this script is attached to a GameObject containing a Camera component
    public Camera cameraToUse;

    //Set of objects, currently there's just 2 sets, one with static objects, and one with dynamic objects
    public class EvaluationSet
    {
        public SmallShadowOptimizer owner;
        public GameObject[] AllGameObjects;
        public MeshRenderer[] AllMeshRenderers;
        public Transform[] allTranforms;
        public Bounds[] allBounds;
        public float[] boundsAsFloats;
        public int[] results;
        public int[] resultsDiff;

        public void OnDisable()
        {
            for (int i = 0; i < AllMeshRenderers.Length; i++)
            {
                if (AllMeshRenderers[i] != null)
                    AllMeshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }
        //Puts bounds variables into the packed array needed for the C++ plugin
        public void UpdateBounds(int index, Bounds bounds)
        {
            boundsAsFloats[index * 6 + 0] = bounds.center.x;
            boundsAsFloats[index * 6 + 1] = bounds.center.y;
            boundsAsFloats[index * 6 + 2] = bounds.center.z;

            boundsAsFloats[index * 6 + 3] = bounds.size.x;
            boundsAsFloats[index * 6 + 4] = bounds.size.y;
            boundsAsFloats[index * 6 + 5] = bounds.size.z;
        }
        public void Initialize(List<GameObject> objects)
        {
            AllGameObjects = objects.ToArray();

            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
            for (int i = 0; i < AllGameObjects.Length; i++)
            {
                GameObject obj = AllGameObjects[i];
                MeshRenderer mr = obj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    meshRenderers.Add(mr);
                }
            }

            AllMeshRenderers = meshRenderers.ToArray();

            List<MeshRenderer> shadowCasters = new List<MeshRenderer>();

            for (int i = 0; i < AllMeshRenderers.Length; i++)
            {
                MeshRenderer MR = AllMeshRenderers[i];
                if (MR.shadowCastingMode == ShadowCastingMode.On)
                    shadowCasters.Add(MR);
            }

            AllMeshRenderers = shadowCasters.ToArray();
            allTranforms = new Transform[AllMeshRenderers.Length];
            allBounds = new Bounds[AllMeshRenderers.Length];
            boundsAsFloats = new float[allBounds.Length * 6];
            results = new int[AllMeshRenderers.Length];
            resultsDiff = new int[results.Length];

            for (int i = 0; i < AllMeshRenderers.Length; i++)
            {
                MeshRenderer meshRenderer = AllMeshRenderers[i];
                allTranforms[i] = meshRenderer.transform;
                results[i] = 1;//by default they all cast shadows
                allBounds[i] = meshRenderer.bounds;

                UpdateBounds(i, allBounds[i]);

                meshRenderer.transform.hasChanged = false;
            }
        }
        //List of objects that need transform updates
        List<int> transformChanges = new List<int>();
        public void UpdateTransforms()
        {
            int numChanges = 0;

            transformChanges.Clear();
            UnityEngine.Profiling.Profiler.BeginSample("transformUpdates");
            numChanges = 0;
            for (int i = 0; i < AllMeshRenderers.Length; i++)
            {
                if (allTranforms[i].hasChanged)
                {
                    transformChanges.Add(i);//[ numChanges ] = i;
                    numChanges++;
                }
            }

            for (int i = 0; i < transformChanges.Count; i++)
            {
                int index = transformChanges[i];
                UpdateBounds(index, AllMeshRenderers[index].bounds);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
        public void Evaluate()
        {
            int diffSize = 0;
            CheckBounds(boundsAsFloats, allBounds.Length, results, owner.SizeThreshold, resultsDiff, ref diffSize);

            for (int i = 0; i < diffSize; i++)
            {
                int objectIndex = resultsDiff[i];
                int castShadows = results[objectIndex];

                if (castShadows == 1)
                    AllMeshRenderers[objectIndex].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                else
                    AllMeshRenderers[objectIndex].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    //Set of static objects which will not move during the lifetime of this script
    EvaluationSet staticSet = new EvaluationSet();
    //Set of dynamic objects that <might> move
    EvaluationSet dynamicSet = new EvaluationSet();

    void Start()
    {
        
    }
    private void OnEnable()
    {
        Initialize();
    }
    
    private void OnDisable()
    {
        staticSet.OnDisable();
        dynamicSet.OnDisable();
    }
    List<GameObject> GatherObjects( Func<GameObject,bool> condition )
    {
        GameObject[] AllGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        List<GameObject> results = new List<GameObject>();

        for (int i = 0; i < AllGameObjects.Length; i++)
        {
            if (condition( AllGameObjects[i]))
            {
                results.Add( AllGameObjects[i] );
            }
        }

        return results;
    }
    void Initialize()
    {
        GameObject[] AllGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

        List<GameObject> staticObjects = GatherObjects((GameObject obj) =>
       {
           if (obj.isStatic)
               return true;
           else
               return false;
       });
        List<GameObject> dynamicObjects = GatherObjects((GameObject obj) =>
        {
            if (!obj.isStatic)
                return true;
            else
                return false;
        });

        staticSet.owner = this;
        staticSet.Initialize( staticObjects );

        dynamicSet.owner = this;
        dynamicSet.Initialize( dynamicObjects );

        if (cameraToUse == null)
            cameraToUse = this.GetComponent<Camera>();

        if (cameraToUse == null )
            cameraToUse = Camera.main;
    }
	
    //List<int> transformChanges = new List<int>();
    
    void Update()
    {
        dynamicSet.UpdateTransforms();
        
        UpdateCpp();
    }
    void UpdateCpp()
    {
        //profil
        float Aspect = (float)Screen.width / (float)Screen.height;
        SetFrustum(Aspect, cameraToUse.fieldOfView, cameraToUse.nearClipPlane, cameraToUse.farClipPlane, cameraToUse.transform.position, cameraToUse.transform.forward, cameraToUse.transform.up);

        staticSet.Evaluate();
        dynamicSet.Evaluate();
    }
}
