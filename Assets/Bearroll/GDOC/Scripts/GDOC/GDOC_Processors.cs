using Bearroll.GDOC_Internal;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bearroll {

    public partial class GDOC {

        bool ProcessGameObject(GameObject go, bool force = false) {

			// Debug.Log(go.name, go);
            // return true to skip children

		    if(!force && !includeDisabledObjects && !go.activeInHierarchy && !kickstart) return false;

			var layer = go.layer;

			if (layer >= 8 && string.IsNullOrEmpty(LayerMask.LayerToName(layer))) {

				if (shouldLogWarnings) {
					Debug.Log(string.Format("{0}: layer {1} doesn't exist, processing as Default.", go.name, layer), go);
				}

				layer = 0;
			}

		    var mode = layerManagementMode[layer];
            var updateMode = layerMovementMode[layer];

		    if (mode == GDOC_ManagementMode.None) return false;
			
		    GDOC_Occludee parentOccludee = null;
			GDOC_Group group = null;

			if (dynamicAnimators) {

				if (go.GetComponent<Animator>() != null) {

					group = go.AddComponent<GDOC_Group>();
					group.overrideUpdateMode = true;
					group.updateMode = GDOC_UpdateMode.Dynamic;

				}

			}

		    var p = go.transform.parent;

		    while (p != null && (parentOccludee == null || group == null)) {
				if (parentOccludee == null) {
					parentOccludee = p.GetComponent<GDOC_Occludee>();
				}
				if (group == null) {
					group = p.GetComponent<GDOC_Group>();
				}
				p = p.parent;
		    }

			if (group != null) {
				if (group.overrideUpdateMode) {
					updateMode = group.updateMode;
				}
			}

            if (parentOccludee != null) {

				if (parentOccludee.isGroup && parentOccludee.GetComponent<LODGroup>() == null) {

					if (shouldLogInfo) {
						
					}

					return true;
				}

                if (parentOccludee.isGeneric) return true;

                if (parentOccludee.isExcludedWithChildren) return true;

            }

            var e = go.GetComponent<GDOC_Occludee>();

			if(e != null) {

			    if (!includeDisabledComponents && !e.enabled && !kickstart) return false;

				if(e.runtimeId != -1) return e.isGroup;

				if(e.isExcludedWithChildren) return true;

				if(e.isExcluded) return false;

			    if (e.isContainer && ignoreContainers) return false;

				return AddOccludee(e, parentOccludee) != -1 && !e.isContainer && !e.isGeneric;

			}

		    if (includeMeshRenderers) {

		        var meshRenderer = go.GetComponent<MeshRenderer>();

		        if (meshRenderer != null) {

		            if (!includeDisabledComponents && !meshRenderer.enabled) return false;

		            IncludeMeshRenderer(meshRenderer, mode, updateMode, parentOccludee);
		            return false;
		        }

		    }

		    if (includePointAndPointLights) {

		        var light = go.GetComponent<Light>();

		        if (light != null && (light.type == LightType.Point || light.type == LightType.Spot)) {

		            if (!includeDisabledComponents && !light.enabled) return false;

		            IncludeSpotOrPointLight(light, updateMode, parentOccludee);
		            return false;
		        }

		    }

		    if (includeLODGroups) {

		        var lodGroup = go.GetComponent<LODGroup>();

		        if (lodGroup != null) {

		            if (!includeDisabledComponents && !lodGroup.enabled) return false;

		            return IncludeLODGroup(lodGroup, mode, updateMode, parentOccludee);
		        }

		    }

		    if (includeParticleSystems) {

		        var particleSystem = go.GetComponent<ParticleSystem>();

		        if (particleSystem != null) {
		            return IncludeParticleSystem(particleSystem, updateMode, parentOccludee);
		        }

		    }

	        if (includeReflectionProbes) {

	            var probe = go.GetComponent<ReflectionProbe>();

	            if (probe != null) {

	                if (!includeDisabledComponents && !probe.enabled) return false;

	                IncludeReflectionProbe(probe, updateMode, parentOccludee);
	                return false;
	            }

	        }

		    return false;

		}

        bool IncludeReflectionProbe(ReflectionProbe probe, GDOC_UpdateMode movementMode = GDOC_UpdateMode.Static, GDOC_Occludee parentOccludee = null) {

            var e = probe.gameObject.AddComponent<GDOC_Occludee>();

            e.mode = GDOC_OccludeeMode.Generic;
            e.movementMode = movementMode;
            e.isTemporary = true;
            e.center = probe.center;
            e.extents = probe.size * 0.5f;
            e.disablePrediction = true;
            // e.allowFullDisable = false;

            if (probe.mode == ReflectionProbeMode.Realtime && probe.refreshMode == ReflectionProbeRefreshMode.OnAwake) {
                probe.RenderProbe();
                probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            }

            e.Init();

            return AddOccludee(e, parentOccludee) != -1;

        }

        bool IncludeSpotOrPointLight(Light light, GDOC_UpdateMode movementMode = GDOC_UpdateMode.Static, GDOC_Occludee parentOccludee = null) {

            var e = light.gameObject.AddComponent<GDOC_Occludee>();

            e.mode = GDOC_OccludeeMode.Generic;
            e.movementMode = movementMode;
            e.isTemporary = true;
            e.isShadowSource = light.shadows != LightShadows.None;
            e.disablePrediction = true;

            if (light.type == LightType.Point) {
                e.center = Vector3.zero;
                e.extents = Vector3.one * light.range;
            }

            e.Init();

            if (light.type == LightType.Spot) {
                e.RecalculateContainerBounds();
            }

            return AddOccludee(e, parentOccludee) != -1;

        }

        bool IncludeMeshRenderer(Renderer renderer, GDOC_ManagementMode mode, GDOC_UpdateMode movementMode, GDOC_Occludee parentOccludee = null) {

            var meshFilter = renderer.GetComponent<MeshFilter>();

            if (meshFilter == null) return false;

            if (meshFilter.sharedMesh == null) return false;

            var e = renderer.gameObject.AddComponent<GDOC_Occludee>();

            e.mode = GDOC_OccludeeMode.MeshRenderer;
            e.movementMode = movementMode;
            e.isTemporary = true;
            e.shadowCastingMode = renderer.shadowCastingMode;
            e.managementMode = mode;

            e.Init();

            var size = e.size.x + e.size.y + e.size.z;

            if (size >= 3) {

                var material = renderer.sharedMaterial;

                if (material != null && material.shader != null) {

                    if (material.shader.renderQueue < 2450) {
                        e.isImportant = true;
                    }

                }

            }

            return AddOccludee(e, parentOccludee) != -1;

        }

        bool IncludeLODGroup(LODGroup lodGroup,  GDOC_ManagementMode mode, GDOC_UpdateMode movementMode = GDOC_UpdateMode.Static, GDOC_Occludee parentOccludee = null) {

            var e = lodGroup.gameObject.AddComponent<GDOC_Occludee>();
     
            lodGroup.RecalculateBounds();

            e.mode = GDOC_OccludeeMode.MeshRendererGroup;
            e.movementMode = movementMode;
            e.isTemporary = true;
            e.managementMode = mode;

			e.GrabLODGroupRenderers();

			e.Init();

            e.isImportant = e.volumeSizeSqr > 2;

			AddOccludee(e, parentOccludee);

			return false;

		}

        bool IncludeParticleSystem(ParticleSystem ps, GDOC_UpdateMode movementMode = GDOC_UpdateMode.Static, GDOC_Occludee parentOccludee = null) {

            var main = ps.main;

            if (!main.loop) return false;

            var e = ps.gameObject.AddComponent<GDOC_Occludee>();

            e.mode = GDOC_OccludeeMode.ParticleSystem;
            e.movementMode = movementMode;
            e.dynamicUpdateInterval = 1;
            e.sizeMultiplier = 1.1f;
            e.isTemporary = true;

            e.Init();

            if (movementMode == GDOC_UpdateMode.Static) {

                var lt = main.startLifetime;
                float lifetime = 0;
                if (lt.mode == ParticleSystemCurveMode.Constant) {
                    lifetime = lt.constant;
                } else if (lt.mode == ParticleSystemCurveMode.Curve) {
                    lifetime = lt.curveMultiplier;
                } else if (lt.mode == ParticleSystemCurveMode.TwoConstants) {
                    lifetime = lt.constantMax;
                } else if (lt.mode == ParticleSystemCurveMode.TwoCurves) {
                    lifetime = lt.curveMultiplier; // hm
                }

                var t = main.duration + lifetime;

                var wasPlaying = ps.isPlaying;

                ps.Simulate(t);

                if (wasPlaying) {
                    ps.Play();
                }

                e.RecalculateContainerBounds(psOmnidirectionalBounds);

                if (e.size.magnitude < 1f && Application.isEditor) {
                    Debug.LogWarning(string.Format("Particle system {0} has very small static bounds", ps.name), ps.gameObject);
                }

            }

            return AddOccludee(e, parentOccludee) != -1;

        }

    }

}