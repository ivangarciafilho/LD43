using UnityEngine;
using System;
using System.Collections.Generic;

namespace MeshCombineStudio
{
    public class ObjectOctree
    {
        public class LODParent
        {
            public GameObject cellGO;
            public Transform cellT;

            public LODGroup lodGroup;
            public LODLevel[] lodLevels;
            public bool hasChanged;
            public int jobsPending;

            public LODParent(int lodCount)
            {
                lodLevels = new LODLevel[lodCount];
                for (int i = 0; i < lodLevels.Length; i++) lodLevels[i] = new LODLevel();
            }

            public void AssignLODGroup(MeshCombiner meshCombiner)
            {
                LOD[] lods = new LOD[lodLevels.Length];
                int lodGroupParentIndex = lods.Length - 1;
                
                for (int i = 0; i < lodLevels.Length; i++)
                {
                    LODLevel lodLevel = lodLevels[i];
                    // Debug.Log(i + " " + lodLevel.newMeshRenderers.Count);
                    lods[i] = new LOD(meshCombiner.lodGroupsSettings[lodGroupParentIndex].lodSettings[i].screenRelativeTransitionHeight, lodLevel.newMeshRenderers.ToArray());
                }

                lodGroup.SetLODs(lods);
                lodGroup.size = meshCombiner.cellSize;
            }

            public void ApplyChanges(MeshCombiner meshCombiner)
            {
                for (int i = 0; i < lodLevels.Length; i++) lodLevels[i].ApplyChanges(meshCombiner);
                hasChanged = false;
            }
        }

        public class LODLevel
        {
            public List<CachedGameObject> cachedGOs = new List<CachedGameObject>();
            public List<MeshObjectsHolder> meshObjectsHolders;
            public List<MeshObjectsHolder> changedMeshObjectsHolders;
            public List<MeshRenderer> newMeshRenderers = new List<MeshRenderer>();
            public int vertCount, objectCount = 0;
            
            public int GetSortMeshIndex(Material mat, bool shadowCastingModeTwoSided, int lightmapIndex)
            {
                int matInstanceID = mat.GetInstanceID();

                for (int i = 0; i < meshObjectsHolders.Count; i++)
                {
                    MeshObjectsHolder meshObjectHolder = meshObjectsHolders[i];
                    // if (mat == null) Debug.Log("Material null");
                    if (meshObjectHolder.mat == null) { continue; }// Debug.Log("Sorted mat null");

                    if (meshObjectHolder.mat.GetInstanceID() == matInstanceID && meshObjectHolder.shadowCastingModeTwoSided == shadowCastingModeTwoSided && meshObjectHolder.lightmapIndex == lightmapIndex) return i;

                    // if (meshObjectHolder.mat.name == mat.name && meshObjectHolder.mat.shader == mat.shader && meshObjectHolder.shadowCastingModeTwoSided == shadowCastingModeTwoSided && meshObjectHolder.lightmapIndex == lightmapIndex &&
                    // (!mat.HasProperty("_MainTex") || (mat.HasProperty("_MainTex") && meshObjectHolder.mat.GetTexture("_MainTex") == mat.GetTexture("_MainTex")))) return i;
                }
                return -1;
            }

            public void ApplyChanges(MeshCombiner meshCombiner)
            {
                for (int i = 0; i < changedMeshObjectsHolders.Count; i++)
                {
                    MeshObjectsHolder meshObjectHolder = changedMeshObjectsHolders[i];
                    meshObjectHolder.hasChanged = false;


                }
                changedMeshObjectsHolders.Clear();
            }
        }

        public class MaxCell : Cell
        {
            static public int maxCellCount;
            public LODParent[] lodParents;
            public List<LODParent> changedLodParents;
            public bool hasChanged;

            public void ApplyChanges(MeshCombiner meshCombiner)
            {
                for (int i = 0; i < changedLodParents.Count; i++) changedLodParents[i].ApplyChanges(meshCombiner);
                changedLodParents.Clear();
                hasChanged = false;
            }
        }

        public class Cell : BaseOctree.Cell
        {
            public Cell[] cells;

            public Cell() { }
            public Cell(Vector3 position, Vector3 size, int maxLevels) : base(position, size, maxLevels) { }

            public CachedGameObject AddObject(Vector3 position, MeshCombiner meshCombiner, CachedGameObject cachedGO, int lodParentIndex, int lodLevel, bool isChangeMode = false)
            {
                if (InsideBounds(position))
                {
                    AddObjectInternal(meshCombiner, cachedGO, position, lodParentIndex, lodLevel, isChangeMode);
                    return cachedGO;
                }
                return null;
            }

            void AddObjectInternal(MeshCombiner meshCombiner, CachedGameObject cachedGO, Vector3 position, int lodParentIndex, int lodLevel, bool isChangeMode)
            {
                if (level == maxLevels)
                {
                    MaxCell thisCell = (MaxCell)this;

                    if (thisCell.lodParents == null) thisCell.lodParents = new LODParent[10];
                    if (thisCell.lodParents[lodParentIndex] == null) thisCell.lodParents[lodParentIndex] = new LODParent(lodParentIndex + 1);

                    LODParent lodParent = thisCell.lodParents[lodParentIndex];
                    LODLevel lod = lodParent.lodLevels[lodLevel];
                    
                    lod.cachedGOs.Add(cachedGO);
                    if (isChangeMode)
                    {
                        if (SortObject(meshCombiner, lod, cachedGO))
                        {
                            if (!thisCell.hasChanged)
                            {
                                thisCell.hasChanged = true;
                                if (meshCombiner.changedCells == null) meshCombiner.changedCells = new List<MaxCell>();
                                meshCombiner.changedCells.Add(thisCell);
                            }
                            if (!lodParent.hasChanged)
                            {
                                lodParent.hasChanged = true;
                                thisCell.changedLodParents.Add(lodParent);
                            }
                        }
                    }

                    lod.objectCount++;

                    lod.vertCount += cachedGO.mesh.vertexCount;
                    return;
                }
                else
                {
                    bool maxCellCreated;
                    int index = AddCell<Cell, MaxCell>(ref cells, position, out maxCellCreated);
                    if (maxCellCreated) MaxCell.maxCellCount++;
                    cells[index].AddObjectInternal(meshCombiner, cachedGO, position, lodParentIndex, lodLevel, isChangeMode);
                }
            }
            
            public void SortObjects(MeshCombiner meshCombiner)
            {
                if (level == maxLevels)
                {
                    MaxCell thisCell = (MaxCell)this;

                    LODParent[] lodParents = thisCell.lodParents;

                    for (int i = 0; i < lodParents.Length; i++)
                    {
                        LODParent lodParent = lodParents[i];
                        if (lodParent == null) continue;

                        for (int j = 0; j < lodParent.lodLevels.Length; j++)
                        {
                            LODLevel lod = lodParent.lodLevels[j];
                            
                            if (lod == null || lod.cachedGOs.Count == 0) return;

                            for (int k = 0; k < lod.cachedGOs.Count; ++k)
                            {
                                CachedGameObject cachedGO = lod.cachedGOs[k];

                                if (!SortObject(meshCombiner, lod, cachedGO))
                                {
                                    Methods.ListRemoveAt(lod.cachedGOs, k--);
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 8; ++i)
                    {
                        if (cellsUsed[i]) cells[i].SortObjects(meshCombiner);
                    }
                }
            }

            public bool SortObject(MeshCombiner meshCombiner, LODLevel lod, CachedGameObject cachedGO, bool isChangeMode = false)
            {
                if (cachedGO.mr == null) return false;

                if (lod.meshObjectsHolders == null) lod.meshObjectsHolders = new List<MeshObjectsHolder>();
                
                Material[] mats = cachedGO.mr.sharedMaterials;

                // TODO check submeshes and material
                int length = Mathf.Min(cachedGO.mesh.subMeshCount, mats.Length);

                for (int l = 0; l < length; l++)
                {
                    Material mat = mats[l];
                    if (mat == null) continue;

                    bool shadowCastingModeTwoSided = (cachedGO.mr.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.TwoSided);
                    int lightmapIndex = meshCombiner.validCopyBakedLighting ? cachedGO.mr.lightmapIndex : -1;

                    int index = lod.GetSortMeshIndex(mat, shadowCastingModeTwoSided, lightmapIndex);
                     
                    MeshObjectsHolder meshObjectHolder;
                    if (index == -1)
                    {
                        meshObjectHolder = new MeshObjectsHolder(cachedGO, mat, l, shadowCastingModeTwoSided, lightmapIndex);
                        lod.meshObjectsHolders.Add(meshObjectHolder);
                    }
                    else
                    {
                        meshObjectHolder = lod.meshObjectsHolders[index];
                        meshObjectHolder.meshObjects.Add(new MeshObject(cachedGO, l));
                    }

                    if (isChangeMode && !meshObjectHolder.hasChanged)
                    {
                        meshObjectHolder.hasChanged = true;
                        lod.changedMeshObjectsHolders.Add(meshObjectHolder);
                    }
                }
                
                return true;
            }

            public void CombineMeshes(MeshCombiner meshCombiner, int lodParentIndex)
            {
                if (level == maxLevels)
                {
                    MaxCell thisCell = (MaxCell)this;
                    
                    LODParent lodParent = thisCell.lodParents[lodParentIndex];
                    if (lodParent == null) return;
                    
                    lodParent.cellGO = new GameObject(meshCombiner.useCells ? "Cell " + bounds.center : "Combined Objects");
                    lodParent.cellT = lodParent.cellGO.transform;
                    lodParent.cellT.position = bounds.center;
                    lodParent.cellT.parent = meshCombiner.lodParentHolders[lodParentIndex].t;

                    if (lodParentIndex > 0)
                    {
                        lodParent.lodGroup = lodParent.cellGO.AddComponent<LODGroup>();
                        lodParent.lodGroup.localReferencePoint = lodParent.cellT.position = bounds.center;
                    }
                    
                    LODLevel[] lods = lodParent.lodLevels;
                    for (int i = 0; i < lods.Length; i++)
                    {
                        LODLevel lod = lodParent.lodLevels[i];
                        if (lod == null || lod.meshObjectsHolders == null) return;

                        GameObject lodGO;
                        Transform lodT = null;

                        if (lodParentIndex > 0)
                        {
                            lodGO = new GameObject("LOD" + i);
                            lodT = lodGO.transform;
                            lodT.parent = lodParent.cellT;
                        }
                        
                        for (int k = 0; k < lod.meshObjectsHolders.Count; ++k)
                        {
                            MeshObjectsHolder sortedMeshes = lod.meshObjectsHolders[k];
                            sortedMeshes.lodParent = lodParent;
                            sortedMeshes.lodLevel = i;
                            MeshCombineJobManager.instance.AddJob(meshCombiner, sortedMeshes, lodParentIndex > 0 ? lodT : lodParent.cellT, bounds.center);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 8; ++i)
                    {
                        if (cellsUsed[i]) cells[i].CombineMeshes(meshCombiner, lodParentIndex);
                    }
                }
            }
            
            public void Draw(MeshCombiner meshCombiner, bool onlyMaxLevel, bool drawLevel0)
            {
                if (!onlyMaxLevel || level == maxLevels || (drawLevel0 && level == 0))
                {
                    Gizmos.DrawWireCube(bounds.center, bounds.size);

                    if (level == maxLevels)
                    {
                        if (meshCombiner.drawMeshBounds)
                        {
                            MaxCell thisCell = (MaxCell)this;

                            LODParent[] lodParents = thisCell.lodParents;

                            for (int i = 0; i < lodParents.Length; i++)
                            {
                                if (lodParents[i] == null) continue;

                                LODLevel[] lods = lodParents[i].lodLevels;

                                Gizmos.color = meshCombiner.activeOriginal ? Color.blue : Color.green;
                                for (int j = 0; j < lods.Length; j++)
                                {
                                    for (int k = 0; k < lods[j].cachedGOs.Count; k++)
                                    {
                                        if (lods[j].cachedGOs[k].mr == null) continue;
                                        Bounds meshBounds = lods[j].cachedGOs[k].mr.bounds;
                                        Gizmos.DrawWireCube(meshBounds.center, meshBounds.size);
                                    }
                                }
                                Gizmos.color = Color.white;
                            }
                            return;
                        }
                    }
                }

                if (cells == null || cellsUsed == null) { return; }
                
                for (int i = 0; i < 8; i++)
                {
                    if (cellsUsed[i]) cells[i].Draw(meshCombiner, onlyMaxLevel, drawLevel0);
                }
            }
        }
    }

    [Serializable]
    public class MeshObjectsHolder
    {
        public Material mat;
        public List<MeshObject> meshObjects = new List<MeshObject>();
        public ObjectOctree.LODParent lodParent;
        public List<CachedGameObject> newCachedGOs;
        public int lodLevel;
        public int lightmapIndex;
        public bool shadowCastingModeTwoSided;
        public bool hasChanged;
        
        public MeshObjectsHolder(CachedGameObject cachedGO, Material mat, int subMeshIndex, bool shadowCastingModeTwoSided, int lightmapIndex)
        {
            // Debug.Log(useForLightmapping);
            this.mat = mat;
            this.shadowCastingModeTwoSided = shadowCastingModeTwoSided;
            this.lightmapIndex = lightmapIndex;
            meshObjects.Add(new MeshObject(cachedGO, subMeshIndex));
        }
    }

    [Serializable]
    public class MeshObject
    {
        public CachedGameObject cachedGO;
        public MeshCache meshCache;
        public int subMeshIndex;
        public Vector3 position, scale;
        public Quaternion rotation;
        public Vector4 lightmapScaleOffset;
        public bool intersectsSurface;
        public int startNewTriangleIndex, newTriangleCount;
        public bool skip;

        public MeshObject(CachedGameObject cachedGO, int subMeshIndex)
        {
            this.cachedGO = cachedGO;
            this.subMeshIndex = subMeshIndex;
            Transform t = cachedGO.t;
            position = t.position;
            rotation = t.rotation;
            scale = t.lossyScale;
            lightmapScaleOffset = cachedGO.mr.lightmapScaleOffset;
        }
    }

    [Serializable]
    public class CachedGameObject
    {
        public GameObject go;
        public Transform t;
        public MeshRenderer mr;
        public MeshFilter mf;
        public Mesh mesh;

        public CachedGameObject(GameObject go, Transform t, MeshRenderer mr, MeshFilter mf, Mesh mesh)
        {
            this.go = go;
            this.t = t;
            this.mr = mr;
            this.mf = mf;
            this.mesh = mesh;
        }

        public CachedGameObject(CachedComponents cachedComponent)
        {
            go = cachedComponent.go;
            t = cachedComponent.t;
            mr = cachedComponent.mr;
            mf = cachedComponent.mf;
            mesh = cachedComponent.mf.sharedMesh;
        }
    }

    [Serializable]
    public class CachedLodGameObject : CachedGameObject
    {
        public Vector3 center;
        public int lodCount, lodLevel;

        public CachedLodGameObject(CachedGameObject cachedGO, int lodCount, int lodLevel) : base(cachedGO.go, cachedGO.t, cachedGO.mr, cachedGO.mf, cachedGO.mesh)
        {
            this.lodCount = lodCount;
            this.lodLevel = lodLevel;
        }
    }
}