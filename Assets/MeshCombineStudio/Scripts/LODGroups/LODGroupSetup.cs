using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCombineStudio
{
    public class LODGroupSetup : MonoBehaviour {

        public MeshCombiner meshCombiner;
        public LODGroup lodGroup;
        public int lodGroupParentIndex;
        public int lodCount;

        LODGroup[] lodGroups;
        
        public void Init(MeshCombiner meshCombiner, int lodGroupParentIndex)
        {
            this.meshCombiner = meshCombiner;
            this.lodGroupParentIndex = lodGroupParentIndex;
            lodCount = lodGroupParentIndex + 1;

            if (lodGroup == null) lodGroup = gameObject.AddComponent<LODGroup>();

            GetSetup();
        }

        void GetSetup()
        {
            LOD[] lods = new LOD[lodGroupParentIndex + 1];

            for (int i = 0; i < lods.Length; i++)
            {
                lods[i] = new LOD();
                lods[i].screenRelativeTransitionHeight = meshCombiner.lodGroupsSettings[lodGroupParentIndex].lodSettings[i].screenRelativeTransitionHeight;
            }

            lodGroup.SetLODs(lods);
        }

        public void ApplySetup()
        {
            // Debug.Log("ApplySetup");
            LOD[] lods = lodGroup.GetLODs();

            if (lodGroups == null) lodGroups = GetComponentsInChildren<LODGroup>();

            if (lods.Length != lodCount) return;
            
            bool lodGroupsAreRemoved = false;

            if (lodGroupParentIndex == 0)
            {
                // Debug.Log("Length " + lodGroups.Length +" " +lods[0].screenRelativeTransitionHeight);
                if (lods[0].screenRelativeTransitionHeight != 0)
                {
                    if (lodGroups == null || lodGroups.Length == 1) AddLODGroupsToChildren();
                }
                else
                {
                    if (lodGroup != null && lodGroups.Length != 1) RemoveLODGroupFromChildren();
                    lodGroupsAreRemoved = true;
                }
            }

            if (meshCombiner != null)
            {
                for (int i = 0; i < lods.Length; i++)
                {
                    meshCombiner.lodGroupsSettings[lodGroupParentIndex].lodSettings[i].screenRelativeTransitionHeight = lods[i].screenRelativeTransitionHeight;
                }
            }

            if (lodGroupsAreRemoved) return;
            
            for (int i = 0; i < lodGroups.Length; i++)
            {
                LOD[] childLods = lodGroups[i].GetLODs();
                
                for (int j = 0; j < childLods.Length; j++)
                {
                    childLods[j].screenRelativeTransitionHeight = lods[j].screenRelativeTransitionHeight;
                }

                lodGroups[i].SetLODs(childLods);
            }

            if (meshCombiner != null) lodGroup.size = meshCombiner.cellSize;
        }

        public void AddLODGroupsToChildren()
        {
            // Debug.Log("Add Lod Groups");
            Transform t = transform;
            List<LODGroup> lodGroupList = new List<LODGroup>();

            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                Debug.Log(child.name);
                LODGroup lodGroup = child.GetComponent<LODGroup>();

                if (lodGroup == null)
                {
                    lodGroup = child.gameObject.AddComponent<LODGroup>();
                    LOD[] lods = new LOD[1];
                    lods[0] = new LOD(0, child.GetComponentsInChildren<MeshRenderer>());
                    lodGroup.SetLODs(lods);
                }

                lodGroupList.Add(lodGroup);
            }

            lodGroups = lodGroupList.ToArray();
        }

        public void RemoveLODGroupFromChildren()
        {
            // Debug.Log("Remove Lod Groups");
            Transform t = transform;

            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                LODGroup lodGroup = child.GetComponent<LODGroup>();
                if (lodGroup != null) DestroyImmediate(lodGroup);
            }

            lodGroups = null;
        }
    }
}
