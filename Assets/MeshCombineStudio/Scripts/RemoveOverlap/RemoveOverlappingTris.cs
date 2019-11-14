using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace MeshCombineStudio
{
    static public class RemoveOverlappingTris
    {
        static public FastList<Triangle3> triangles = new FastList<Triangle3>();

        static FastList<ColliderInfo> collidersInfo = new FastList<ColliderInfo>();
        static FastList<Collider> colliders = new FastList<Collider>(10000);
        static FastList<RaycastHit> hitInfos = new FastList<RaycastHit>(10000);
        static FastList<RaycastHit> hitInfos2 = new FastList<RaycastHit>(10000);
        static RaycastHit hitInfo;

        static HashSet<GameObject> toCombineGos = new HashSet<GameObject>();

        static Triangle3 tri = new Triangle3();

        const byte insideVoxel = 1;
        const byte outsideVoxel = 2;

        struct ColliderInfo
        {
            public GameObject go;
            public int layer;
        }

        static public void RemoveOverlap(Transform t, MeshCombineJobManager.MeshCombineJob meshCombineJob, MeshCache.SubMeshCache newMeshCache, ref byte[] vertexIsInsideCollider)
        {
            if (vertexIsInsideCollider == null) vertexIsInsideCollider = new byte[65534];

            int overlapLayerMask = meshCombineJob.meshCombiner.overlapLayerMask;
            int voxelizeLayer = meshCombineJob.meshCombiner.voxelizeLayer;

            int voxelizeLayerMask = 1 << voxelizeLayer;
            int lodGroupLayer = meshCombineJob.meshCombiner.lodGroupLayer;
            int lodGroupLayerMask = 1 << lodGroupLayer;

            int lodLevel = meshCombineJob.meshObjectsHolder.lodLevel;

            CreateOverlapColliders.EnableLodLevelCollider(lodLevel, lodGroupLayer);

            Vector3[] newVertices = newMeshCache.vertices;
            int[] newTriangles = newMeshCache.triangles;

            List<MeshObject> meshObjects = meshCombineJob.meshObjectsHolder.meshObjects;

            int startIndex = meshCombineJob.startIndex;
            int endIndex = meshCombineJob.endIndex;

            bool queriesHitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            toCombineGos.Clear();
            for (int i = startIndex; i < endIndex; i++)
            {
                toCombineGos.Add(meshObjects[i].cachedGO.go);
            }

            for (int a = startIndex; a < endIndex; a++)
            {
                MeshObject meshObject = meshObjects[a];
                CachedGameObject cachedGO = meshObject.cachedGO;

                GameObject go;

                CreateOverlapColliders.lookupOrigCollider.TryGetValue(cachedGO.go, out go);

                int startTriangleIndex = meshObject.startNewTriangleIndex;
                int endTriangleIndex = meshObject.newTriangleCount + startTriangleIndex;

                Bounds bounds = cachedGO.mr.bounds;
                int oldLayer = 0;

                if (go)
                {
                    oldLayer = go.layer;
                    go.layer = voxelizeLayer;
                }

                colliders.SetCount(Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, colliders.items, Quaternion.identity, overlapLayerMask));

                if (go) go.layer = oldLayer;

                // Debug.Log("collider Count " + colliders.Count);

                if (colliders.Count == 0) continue;

                collidersInfo.SetCount(colliders.Count);

                for (int i = 0; i < colliders.Count; i++)
                {
                    GameObject colliderGo = colliders.items[i].gameObject;
                    collidersInfo.items[i] = new ColliderInfo() { layer = colliderGo.layer, go = colliderGo };
                    colliderGo.layer = voxelizeLayer;
                }

                // Debug.Log("start " + startTriangleIndex + " end " + endTriangleIndex);

                for (int i = startTriangleIndex; i < endTriangleIndex; i += 3)
                {
                    int vertIndexA = newTriangles[i];
                    if (vertIndexA == -1) continue;

                    byte isInsideVoxel = vertexIsInsideCollider[vertIndexA];

                    if (isInsideVoxel != outsideVoxel)
                    {
                        tri.a = t.TransformPoint(newVertices[vertIndexA]);

                        hitInfos.SetCount(Physics.RaycastNonAlloc(tri.a, Vector3.up, hitInfos.items, Mathf.Infinity, voxelizeLayerMask));

                        if (!AnythingInside()) { vertexIsInsideCollider[vertIndexA] = outsideVoxel; continue; }

                        tri.b = t.TransformPoint(newVertices[newTriangles[i + 1]]);
                        tri.c = t.TransformPoint(newVertices[newTriangles[i + 2]]);

                        if (LinecastAll(tri.a, tri.b, voxelizeLayerMask) && IntersectAny()) continue;
                        if (LinecastAll(tri.b, tri.c, voxelizeLayerMask) && IntersectAny()) continue;
                        if (LinecastAll(tri.c, tri.a, voxelizeLayerMask) && IntersectAny()) continue;
                        if (LinecastAll(tri.a, (tri.b + tri.c) * 0.5f, voxelizeLayerMask) && IntersectAny()) continue;
                        if (LinecastAll(tri.b, (tri.c + tri.a) * 0.5f, voxelizeLayerMask) && IntersectAny()) continue;
                        if (LinecastAll(tri.c, (tri.a + tri.b) * 0.5f, voxelizeLayerMask) && IntersectAny()) continue;

                        //tri.Calc();
                        //Vector3 origin = tri.a + (tri.dirAb / 2) + ((tri.c - tri.h1) / 2);

                        //if (Physics.CheckBox(origin, new Vector3(0.05f, tri.h, tri.ab) / 2, Quaternion.LookRotation(tri.dirAb, tri.dirAc), voxelizeLayerMask))
                        //{
                        //    colliderGO.layer = oldLayer;
                        //    continue;
                        //}

                        if (CreateOverlapColliders.foundLodGroup && AreAllHitInfosALodGroup() && !IsOneColliderGOInToCombineGos() && !CheckAnyInsideOfLodGroups(lodGroupLayerMask, lodLevel)) continue;

                        meshCombineJob.trianglesRemoved += 3;
                        newTriangles[i] = -1;
                    }
                }

                for (int i = 0; i < colliders.Count; i++)
                {
                    ColliderInfo colliderInfo = collidersInfo.items[i];
                    colliderInfo.go.layer = colliderInfo.layer;
                }
            }

            Array.Clear(vertexIsInsideCollider, 0, newVertices.Length);
            // Debug.Log("Removed " + meshCombineJob.trianglesRemoved);

            newMeshCache.triangles = newTriangles;
            Physics.queriesHitBackfaces = queriesHitBackfaces;
        }

        static bool CheckAnyInsideOfLodGroups(int layerMask, int lodLevel)
        {
            bool inside = false;

            for (int i = 0; i < hitInfos.Count; i++)
            {
                Collider collider = hitInfos.items[i].collider;

                CreateOverlapColliders.LodInfo lodInfo;
                CreateOverlapColliders.lodInfoLookup.TryGetValue(collider, out lodInfo);

                lodInfo.SetActiveOtherLodLevels(lodLevel);

                if (Physics.Raycast(tri.a, Vector3.up, out hitInfo, Mathf.Infinity, layerMask))
                {
                    if (Vector3.Dot(Vector3.up, hitInfo.normal) <= 0)
                    {
                        lodInfo.SetActiveOnlyLodLevel(lodLevel); continue;
                    }

                    if (Linecast(tri.a, tri.b, layerMask) || Linecast(tri.b, tri.c, layerMask) || Linecast(tri.c, tri.a, layerMask) ||
                        Linecast(tri.a, (tri.b + tri.c) * 0.5f, layerMask) || Linecast(tri.b, (tri.c + tri.a) * 0.5f, layerMask) || Linecast(tri.c, (tri.a + tri.b) * 0.5f, layerMask))
                    {
                        lodInfo.SetActiveOnlyLodLevel(lodLevel); continue;
                    }
                }
                else { lodInfo.SetActiveOnlyLodLevel(lodLevel); continue; }

                lodInfo.SetActiveOnlyLodLevel(lodLevel);
                inside = true;
                break;
            }

            return inside;
        }

        static bool IsOneColliderGOInToCombineGos()
        {
            for (int i = 0; i < hitInfos.Count; i++)
            {
                GameObject go = hitInfos.items[i].collider.gameObject;
                GameObject toCombineGO;
                CreateOverlapColliders.lookupColliderOrig.TryGetValue(go, out toCombineGO);

                if (toCombineGos.Contains(toCombineGO))
                {
                    return true;
                }
            }

            return false;
        }

        static bool AreAllHitInfosALodGroup()
        {
            for (int i = 0; i < hitInfos.Count; i++)
            {
                if (hitInfos.items[i].collider.name[0] != 'L') return false;
            }

            return true;
        }

        static bool AnythingInside()
        {
            for (int i = 0; i < hitInfos.Count; i++)
            {
                if (Vector3.Dot(Vector3.up, hitInfos.items[i].normal) < 0) hitInfos.RemoveAt(i--);
            }
            return hitInfos.Count > 0;
        }

        static bool Linecast(Vector3 p1, Vector3 p2, int layerMask)
        {
            Vector3 dir = p2 - p1;
            return Physics.Raycast(p1, dir, dir.magnitude, layerMask);
        }

        static bool LinecastAll(Vector3 p1, Vector3 p2, int layerMask)
        {
            Vector3 dir = p2 - p1;
            hitInfos2.SetCount(Physics.RaycastNonAlloc(p1, dir, hitInfos2.items, dir.magnitude, layerMask));
            return hitInfos2.Count > 0;
        }

        static bool IntersectAny()
        {
            for (int i = 0; i < hitInfos.Count; i++)
            {
                Collider collider = hitInfos.items[i].collider;

                for (int j = 0; j < hitInfos2.Count; j++)
                {
                    if (collider == hitInfos2.items[j].collider)
                    {
                        hitInfos.RemoveAt(i--);
                        break;
                    }
                }
            }

            return hitInfos.Count == 0;
        }
    }
}