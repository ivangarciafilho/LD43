using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCombineStudio
{
    [ExecuteInEditMode]
    public class CreateOverlapColliders : MonoBehaviour
    {
        public LayerMask layerMask;

        public bool create;
        public bool destroy;

        public GameObject newGO;
        public int lodLevel = 0;
        public bool setLayer;

        static public bool foundLodGroup;
        static public Dictionary<GameObject, GameObject> lookupOrigCollider = new Dictionary<GameObject, GameObject>();
        static public Dictionary<GameObject, GameObject> lookupColliderOrig = new Dictionary<GameObject, GameObject>();
        static public Dictionary<Collider, LodInfo> lodInfoLookup = new Dictionary<Collider, LodInfo>();
        static FastList<LodInfo> lodInfos = new FastList<LodInfo>();
        static FastList<GameObject> selectGos = new FastList<GameObject>();
        static HashSet<Mesh> lodGroupMeshes = new HashSet<Mesh>();

        static int overlapLayer;

        void Update()
        {
            if (create)
            {
                create = false;

                Create(transform, layerMask, 4, ref newGO);
            }
            if (destroy)
            {
                destroy = false;
                DestroyOverlapColliders(newGO);
            }
            if (setLayer)
            {
                setLayer = false;
                overlapLayer = 0;
                EnableLodLevelCollider(lodLevel, 4);
            }
        }

        static FastList<Collider> colliders = new FastList<Collider>();

        static public void SaveCollidersState(LayerMask layerMask)
        {
            var colliders = Methods.SearchAllScenes<Collider>(false);

            int layerMaskValue = layerMask.value;

            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders.items[i];
                GameObject go = collider.gameObject;

                if (collider.enabled && go.activeInHierarchy && Methods.LayerMaskContainsLayer(layerMaskValue, go.layer))
                {
                    collider.enabled = false;
                    CreateOverlapColliders.colliders.Add(collider);
                }
            }
        }

        static public void RestoreCollidersState()
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders.items[i];
                if (collider) collider.enabled = true;
            }

            colliders.Clear();
        }

        static public void EnableLodLevelCollider(int lodLevel, int lodGroupLayer)
        {
            for (int i = 0; i < lodInfos.Count; i++)
            {
                LodInfo lodInfo = lodInfos.items[i];
                lodInfo.SetActiveOnlyLodLevel(lodLevel);
                lodInfo.SetLayerLodLevel(lodLevel, overlapLayer, lodGroupLayer);
            }
        }

        public class LodInfo
        {
            public FastList<LodLevel> lodLevels = new FastList<LodLevel>();

            public void SetActiveOnlyLodLevel(int lodLevel)
            {
                for (int i = 0; i < lodLevels.Count; i++)
                {
                    lodLevels.items[i].SetCollidersActive(i == lodLevel);
                }
            }

            public void SetActiveOtherLodLevels(int excludeLevel)
            {
                for (int i = 0; i < lodLevels.Count; i++)
                {
                    lodLevels.items[i].SetCollidersActive(i != excludeLevel);
                }
            }

            public void SetLayerLodLevel(int lodLevel, int layer, int otherLayer)
            {
                for (int i = 0; i < lodLevels.Count; i++)
                {
                    lodLevels.items[i].SetLayer(lodLevel == i ? layer : otherLayer);
                }
            }

            public void CreateLodGroupColliders(LODGroup lodGroup, Transform parentT)
            {
                var lodGroupGO = new GameObject("L_" + lodGroup.name);
                Transform lodGroupT = lodGroupGO.transform;
                lodGroupT.parent = parentT;

                LOD[] lods = lodGroup.GetLODs();

                bool meshColliderCreated = false;

                for (int i = 0; i < lods.Length; i++)
                {
                    LOD lod = lods[i];
                    LodLevel lodLevel = new LodLevel();

                    Renderer[] rs = lod.renderers;

                    for (int j = 0; j < rs.Length; j++)
                    {
                        Renderer r = rs[j];
                        GameObject go = r.gameObject;

                        if (r.enabled && go.activeInHierarchy)
                        {
                            MeshFilter mf = go.GetComponent<MeshFilter>();
                            if (mf == null) continue;

                            Mesh mesh = mf.sharedMesh;
                            if (mesh == null) continue;

                            meshColliderCreated = true;
                            MeshCollider mc = CreateMeshCollider(mf, lodGroupT, "L" + i + "_");

                            lodLevel.colliders.Add(mc);
                            lodLevel.gos.Add(mc.gameObject);
                            lodInfoLookup.Add(mc, this);
                            lodGroupMeshes.Add(mesh);
                        }
                    }

                    lodLevels.Add(lodLevel);
                }

                if (meshColliderCreated) lodInfos.Add(this);
            }
        }

        public class LodLevel
        {
            public FastList<Collider> colliders = new FastList<Collider>();
            public FastList<GameObject> gos = new FastList<GameObject>();

            public void SetCollidersActive(bool active)
            {
                for (int i = 0; i < colliders.Count; i++)
                {
                    colliders.items[i].enabled = active;
                }
            }

            public void SetLayer(int layer)
            {
                for (int i = 0; i < gos.Count; i++)
                {
                    gos.items[i].layer = layer;
                }
            }
        }

        static public bool IsAnythingOnFreeLayers(int insideLayer, int lodGroupLayer)
        {
            if (insideLayer == lodGroupLayer)
            {
                Debug.Log("`Free Layer 1` and `Free Layer 2` cannot be the same, please select another free layer that has no active colliders on it");
                return true;
            }

            FastList<Collider> colliders = Methods.SearchAllScenes<Collider>(false);

            for (int i = 0; i < colliders.Count; i++)
            {
                GameObject go = colliders.items[i].gameObject;

                if (go.layer == insideLayer || go.layer == lodGroupLayer)
                {
                    selectGos.Add(go);
                }
            }

            if (selectGos.Count > 0)
            {
#if UNITY_EDITOR
                UnityEditor.Selection.objects = selectGos.ToArray();
#endif

                Debug.Log("There are Colliders active on the free layers, please make sure they are empty otherwise overlap removal won't work correctly. Combining aborted...");
            }

            selectGos.Clear();

            return selectGos.Count > 0;
        }

        static public void Create(Transform parentT, LayerMask overlapLayerMask, int lodGroupLayer, ref GameObject overlapCollidersGO)
        {
            lookupOrigCollider.Clear();
            lookupColliderOrig.Clear();
            lodInfoLookup.Clear();
            lodGroupMeshes.Clear();
            lodInfos.Clear();

            SaveCollidersState(overlapLayerMask);

            int layerMaskValue = overlapLayerMask.value;

            if (layerMaskValue == 0) return;
            overlapLayer = Methods.GetFirstLayerInLayerMask(layerMaskValue);

            overlapCollidersGO = new GameObject("Overlap Colliders");
            Transform newT = overlapCollidersGO.transform;
            newT.parent = parentT;

            var lodGroups = Methods.SearchAllScenes<LODGroup>(false);

            for (int i = 0; i < lodGroups.Count; i++)
            {
                LODGroup lodGroup = lodGroups.items[i];
                GameObject go = lodGroup.gameObject;

                if (!go.activeInHierarchy || !Methods.LayerMaskContainsLayer(layerMaskValue, go.layer))
                {
                    lodGroups.RemoveAt(i--);
                    continue;
                }

                LodInfo lodInfo = new LodInfo();
                lodInfo.CreateLodGroupColliders(lodGroup, newT);
            }

            if (lodInfos.Count > 0) foundLodGroup = true;

            var mrs = Methods.SearchAllScenes<MeshRenderer>(false);
            FastList<MeshFilter> mfs = new FastList<MeshFilter>();

            for (int i = 0; i < mrs.Count; i++)
            {
                MeshRenderer mr = mrs.items[i];
                GameObject go = mr.gameObject;

                if (mr.enabled && go.activeInHierarchy && Methods.LayerMaskContainsLayer(layerMaskValue, go.layer))
                {
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    if (mf == null) continue;

                    Mesh mesh = mf.sharedMesh;
                    if (mesh == null || lodGroupMeshes.Contains(mesh)) continue;

                    mfs.Add(mf);
                }
            }

            for (int i = 0; i < mfs.Count; i++)
            {
                MeshFilter mf = mfs.items[i];
                CreateMeshCollider(mf, newT, "_");
            }

            #if !UNITY_2017
            Physics.SyncTransforms();
            #endif
        }

        static MeshCollider CreateMeshCollider(MeshFilter mf, Transform parentT, string prefixName)
        {
            Transform t = mf.transform;
            GameObject go = mf.gameObject;

            var colliderGO = new GameObject(prefixName + t.name);
            Transform colliderT = colliderGO.transform;
            colliderT.parent = parentT;

            colliderGO.layer = go.layer;

            lookupOrigCollider.Add(go, colliderGO);
            lookupColliderOrig.Add(colliderGO, go);

            CopyTransform(t, colliderT);

            var mc = colliderGO.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.hideFlags = HideFlags.HideInHierarchy;

            return mc;
        }

        static public void DestroyOverlapColliders(GameObject go)
        {
            if (go) GameObject.DestroyImmediate(go);
            RestoreCollidersState();
        }

        static public void CopyTransform(Transform st, Transform dt)
        {
            dt.position = st.position;
            dt.rotation = st.rotation;
            dt.localScale = st.lossyScale;
        }
    }
}
