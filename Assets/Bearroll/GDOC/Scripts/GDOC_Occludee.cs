using System;
using System.Collections;
using System.Collections.Generic;
using Bearroll.GDOC_Internal;
using UnityEngine;
using UnityEngine.Rendering;
using Mono.Simd;

namespace Bearroll {

    [ExecuteInEditMode]
    [SelectionBase]
    public partial class GDOC_Occludee: MonoBehaviour {

        public GDOC_OccludeeMode mode = GDOC_OccludeeMode.Excluded;
        public GDOC_UpdateMode movementMode = GDOC_UpdateMode.Static;

        public bool canChangeRotation = true;
        public bool canChangeScale = false;

        [Range(0.1f, 10f)]
        public float sizeMultiplier = 1f;

        public GDOC_ManagementMode managementMode = GDOC_ManagementMode.Full;
        public ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;

        public bool isImportant = false;
        public bool isShadowSource = false;
        public bool disablePrediction = false;
        public bool allowFullDisable = true;
        public bool withChildren = true;

        [System.NonSerialized]
        public int runtimeId = -1;

        public bool isActive {
            get { return runtimeId != -1; }
        }

        public GDOC_Error initError { get; private set; }

        public int currentState { get; private set; }

        public float dynamicUpdateInterval = 0.1f;

        [SerializeField]
        new Renderer renderer;

        [SerializeField]
        public List<Renderer> renderers;

        [SerializeField]
        public List<Renderer> noShadowRenderers;
        [SerializeField]
        public List<Renderer> shadowOnlyRenderers;

        [SerializeField]
        new ParticleSystem particleSystem;

        Transform t;
        Vector4f lastPosition;
        Vector4f lastRotation;
        Vector3 offset;
        Vector3 originalScale = Vector3.one;
        float lastDynamicUpdate;

        static Vector3 minExtents = new Vector3(0.1f, 0.1f, 0.1f);

        public Vector3 center = Vector3.zero;
        public Vector3 extents = Vector3.one;

        void Awake() {

            OnRemove();

        }

        void OnEnable() {

            Init();

            if(dynamicUpdateInterval > 0) {
                // to prevent objects with same internal updating at once
                lastDynamicUpdate = Time.unscaledTime - UnityEngine.Random.Range(0, dynamicUpdateInterval);
            }
        }   

        public void OnRemove() {
            lastDynamicUpdate = float.NegativeInfinity;
            runtimeId = -1;
            lastPosition = Vector4f.Pi;
        }


        public void Init() {

            if(t == null) {
                t = GetComponent<Transform>();
            }

			currentState = -1;

            initError = GDOC_Error.None;

            if(mode == GDOC_OccludeeMode.MeshRenderer) {

                if(renderer == null) {
                    renderer = GetComponent<MeshRenderer>();
                }

                if(renderer == null) {
                    initError = GDOC_Error.RendererNotFound;
                    return;
                }

            } else if(mode == GDOC_OccludeeMode.MeshRendererGroup) {

                /*
                if(managedRenderersCount == 0) {
                    initError = GDOC_Error.NoRenderers;
                    return;
                }
                */

            } else if(mode == GDOC_OccludeeMode.ParticleSystem) {

                if(particleSystem == null) {
                    particleSystem = gameObject.GetComponent<ParticleSystem>();
                }

                if(particleSystem == null) {
                    initError = GDOC_Error.ParticleSystemNotFound;
                    return;
                }

                renderer = particleSystem.GetComponent<ParticleSystemRenderer>();

            }

            UpdateBounds();

        }

		void AddRendererToGroup(Renderer e) {

			if(e.shadowCastingMode == ShadowCastingMode.ShadowsOnly) {

				if(shadowOnlyRenderers == null) {
					shadowOnlyRenderers = new List<Renderer>();
				}

				shadowOnlyRenderers.Add(e);

			} else if(e.shadowCastingMode == ShadowCastingMode.Off) {

				if(noShadowRenderers == null) {
					noShadowRenderers = new List<Renderer>();
				}

				noShadowRenderers.Add(e);

			} else {

				if(renderers == null) {
					renderers = new List<Renderer>();
				}

				renderers.Add(e);
			}

		}

        public void GrabAllChildRenderers() {

            if(renderers != null) {
                renderers.Clear();
            }
            if(noShadowRenderers != null) {
                noShadowRenderers.Clear();
            }
            if(shadowOnlyRenderers != null) {
                shadowOnlyRenderers.Clear();
            }

            foreach(var e in GetComponentsInChildren<MeshRenderer>()) {

				if (!e.gameObject.activeInHierarchy) continue;

				if (!e.enabled) continue;

				AddRendererToGroup(e);
			}

            UpdateBounds();

        }

		public void GrabLODGroupRenderers(bool exclude = true) {

			if(renderers != null) {
				renderers.Clear();
			}
			if(noShadowRenderers != null) {
				noShadowRenderers.Clear();
			}
			if(shadowOnlyRenderers != null) {
				shadowOnlyRenderers.Clear();
			}

			var lodGroup = GetComponent<LODGroup>();

			if (lodGroup == null) return;

			foreach (var lod in lodGroup.GetLODs()) {

				foreach (var e in lod.renderers) {

					if (e == null) continue;

					AddRendererToGroup(e);

					if (exclude) {

						var occludee = e.GetComponent<GDOC_Occludee>();

						if (occludee == null) {
							occludee = e.gameObject.AddComponent<GDOC_Occludee>();
						}

						occludee.mode = GDOC_OccludeeMode.Excluded;
						occludee.withChildren = false;
					}

				}
			}

			UpdateBounds();

		}

        public void RecalculateContainerBounds(bool omnidirectional = false) {

            var bounds = new Bounds(transform.position, minExtents);

            foreach(var e in GetComponentsInChildren<Renderer>()) {

                if(!e.gameObject.activeInHierarchy)
                    continue;

                if(!e.enabled)
                    continue;

                bounds.Encapsulate(e.bounds);

            }

            foreach(var e in GetComponentsInChildren<Light>()) {

                if(!e.gameObject.activeInHierarchy)
                    continue;

                if(!e.enabled)
                    continue;

                if(e.type == LightType.Point) {

                    var b = new Bounds(e.transform.position, Vector3.one * e.range * 2);
                    bounds.Encapsulate(b);

                } else if(e.type == LightType.Spot) {

                    var z = e.range;
                    var xy = z * Mathf.Tan(Mathf.Deg2Rad * e.spotAngle * 0.5f) * 2;
                    var localBounds = new Bounds(Vector3.forward * z * 1, new Vector3(xy, xy, 0.01f));

                    bounds.Encapsulate(GDOC_Utils.TransformBoundsToWorldSpace(e.transform, localBounds));

                }

            }

            center = bounds.center - transform.position;
            extents = bounds.extents;
            originalScale = transform.localScale;

            if(omnidirectional) {

                extents.x += Mathf.Abs(center.x) * 0.5f;
                extents.y += Mathf.Abs(center.y) * 0.5f;
                extents.z += Mathf.Abs(center.z) * 0.5f;

                center = Vector3.zero;

                var s = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));

                extents.x = s;
                extents.y = s;
                extents.z = s;

            }

            UpdateBounds();

        }

        void SetRendererState(Renderer renderer, int state) {

            if(renderer == null) return;

            if(managementMode == GDOC_ManagementMode.ShadowsOnly) {

                if(shadowCastingMode != ShadowCastingMode.Off) {

					renderer.shadowCastingMode = (state == 1 || state == 2) ? shadowCastingMode : ShadowCastingMode.Off;

				}

            } else if(managementMode == GDOC_ManagementMode.WithoutShadows) {

                renderer.enabled = state > 0;

            } else {

                if(shadowCastingMode == ShadowCastingMode.ShadowsOnly) {

                    renderer.enabled = state == 1 || state == 2;

                } else if(shadowCastingMode == ShadowCastingMode.Off) {

                    renderer.enabled = state == 1 || state == 3;

                } else {

                    if(state == 0) {
                        renderer.enabled = false;
                    } else {
                        renderer.enabled = true;

						if (state == 1) {
							renderer.shadowCastingMode = shadowCastingMode;
						} else if (state == 2) {
							renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
						} else if (state == 3) {
							renderer.shadowCastingMode = ShadowCastingMode.Off;
						}

					}

                }

            }

        }

        public void SetVisibleState(int state, bool allowFullDisable = false) {

            // it shouldn't happen but whatever
            if(managementMode == GDOC_ManagementMode.None) return;

            if(managementMode == GDOC_ManagementMode.ShadowsOnly) {

                if(state == 1)
                    state = 2;
                if(state == 3)
                    state = 0;

            } else if(managementMode == GDOC_ManagementMode.WithoutShadows) {

                if(state == 2)
                    state = 0;
                if(state == 1)
                    state = 3;

            }

            allowFullDisable = allowFullDisable && this.allowFullDisable;

            currentState = state;

            if(isGeneric) {
                gameObject.SetActive(state == 1 || state == 3);
                return;
            }

            if(isContainer)
                return;

            if(state == 0 && allowFullDisable && canBeDisabled) {
                gameObject.SetActive(false);
                return;
            }

            if(state > 0 && !gameObject.activeSelf) {
                gameObject.SetActive(true);
            }

            if(isParticleSystem) {

				if (particleSystem == null) {
					return;
				}

                renderer.enabled = state == 1 || state == 3;

			} else if(isRenderer) {

				if (renderer == null) {
					return;
				}

                SetRendererState(renderer, state);

            } else if(isRendererGroup) {

                var main = state == 1 || state == 3;
                var shadow = state == 1 || state == 2;

                if(renderers != null) {

                    for(var i = 0; i < renderers.Count; i++) {

						if (shouldManageComponents) {
							renderers[i].enabled = state > 0;
						}

						if (shouldManageShadows) {
							if (main && !shadow) {
								renderers[i].shadowCastingMode = ShadowCastingMode.Off;
							} else if (main && shadow) {
								renderers[i].shadowCastingMode = ShadowCastingMode.On;
							} else {
								renderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
							}
						}
						
					}
                }

                if(noShadowRenderers != null && shouldManageComponents) {
                    for(var i = 0; i < noShadowRenderers.Count; i++) {
                        noShadowRenderers[i].enabled = main;
                    }
                }

                if(shadowOnlyRenderers != null && shouldManageComponents) {
                    for(var i = 0; i < shadowOnlyRenderers.Count; i++) {
                        shadowOnlyRenderers[i].enabled = shadow;
                    }
                }

            }

        }

        public bool TryUpdateBounds(float dynamicStep, float time) {

            if(isStatic)
                return false;

            if(time < lastDynamicUpdate + dynamicUpdateInterval)
                return false;

            lastDynamicUpdate = time;

            if(isParticleSystem) {
                UpdateBounds();
                return true;
            }

            dynamicStep *= dynamicStep;

            var r = transform.rotation;
            var newRot = new Vector4f(r.x, r.y, r.z, r.w);

            var rf = newRot == lastRotation;

            if(!rf) {
                var d = newRot - lastRotation;
                d *= d;
                rf = d.X < dynamicStep && d.Y < dynamicStep && d.Z < dynamicStep && d.W < dynamicStep;
            }

            if(rf) {

                var p = transform.position;
                var newPos = new Vector4f(p.x, p.y, p.z, 0);

                var pf = newPos == lastPosition;

                if(!pf) {

                    var d = newPos - lastPosition;
                    d *= d;

                    if(d.X < dynamicStep && d.Y < dynamicStep && d.Z < dynamicStep)
                        return false;

                }

                position = p + offset;

                lastPosition = newPos;

                return true;

            }

            lastRotation = newRot;

            UpdateBounds();

            return true;

        }

        void EnsapculateRenderers(ref bool initDone, ref Bounds bounds, List<Renderer> renderers) {

            if(renderers == null)
                return;

            if(renderers.Count == 0)
                return;

            if(!initDone) {
                bounds = renderers[0].bounds;
                initDone = true;
            }

            for(var i = 1; i < renderers.Count; i++) {
                bounds.Encapsulate(renderers[i].bounds);
            }

        }

        public void UpdateBounds() {

            if(hasCustomBounds) {

                position = transform.position + GDOC_Utils.ApplyScale(center, transform.localScale, originalScale);
                size = GDOC_Utils.ApplyScale(extents, transform.localScale, originalScale) * sizeMultiplier;
                offset = center;

            } else {

                var bounds = new Bounds(Vector3.zero, minExtents);

                if(isParticleSystem && renderer != null) {

                    bounds = renderer.bounds;

                } else if(isRenderer && renderer != null) {

                    bounds = renderer.bounds;

                } else if(isRendererGroup) {

                    bounds = new Bounds();
                    var initDone = false;

                    EnsapculateRenderers(ref initDone, ref bounds, renderers);
                    EnsapculateRenderers(ref initDone, ref bounds, noShadowRenderers);
                    EnsapculateRenderers(ref initDone, ref bounds, shadowOnlyRenderers);

                } else {
                    return;
                }

                position = bounds.center;
                size = bounds.extents * sizeMultiplier;
                offset = position - transform.position;

            }

            size = Vector3.Max(size, minExtents);

            volumeSizeSqr = size.sqrMagnitude;
            this.bounds = new Bounds(position, size);

        }

        public Vector3 position { get; private set; }

        public Vector3 size { get; private set; }

        public float volumeSizeSqr { get; private set; }

        public Bounds bounds { get; private set; }

        public bool isTemporary { get; set; }

        public bool isStatic {
            get { return movementMode == GDOC_UpdateMode.Static; }
        }

        public bool isDynamic {
            get { return movementMode > GDOC_UpdateMode.Static; }
        }

        public bool canBeDisabled {
            get { return isStatic || !isParticleSystem; }
        }

        public bool isExcluded {
            get { return mode == GDOC_OccludeeMode.Excluded; }
        }

        public bool isExcludedWithChildren {
            get { return isExcluded && withChildren; }
        }

        public bool isMeshRendererGroup {
            get {
                return mode == GDOC_OccludeeMode.MeshRendererGroup;
            }
        }

        public bool isRendererGroup {
            get { return isMeshRendererGroup; }  // more options later 
        }

        public bool isPotentialShadowReceiver {
            get { return isMeshRenderer || isMeshRendererGroup; }
        }

        public bool isMeshRenderer {
            get { return mode == GDOC_OccludeeMode.MeshRenderer; }
        }

        public bool isRenderer {
            get { return isMeshRenderer; } // more options later 
        }

        public bool isRendererOrGroup {
            get { return isRenderer || isRendererGroup; }
        }

        public bool isGroup {
            get { return isRendererGroup; }
        }

        public bool isGeneric {
            get { return mode == GDOC_OccludeeMode.Generic; }
        }

        public bool isParticleSystem {
            get { return mode == GDOC_OccludeeMode.ParticleSystem; }
        }

        public bool isGroupOrContainer {
            get { return isGroup || isContainer; }
        }

        public bool hasCustomBounds {
            get { return isContainer || isGeneric || (isParticleSystem && isStatic); }
        }

		public bool shouldManageComponents {
			get { return managementMode == GDOC_ManagementMode.Full || managementMode == GDOC_ManagementMode.WithoutShadows; }
		}

		public bool shouldManageShadows {
			get { return managementMode == GDOC_ManagementMode.Full || managementMode == GDOC_ManagementMode.ShadowsOnly; }
		}

        public bool isContainer {
            get {
                return false; /* mode == GDOC_OccludeeMode.Container; */
            }
        }

        public int managedRenderersCount {
            get {

                if (isRendererGroup) {

                    var count = 0;

                    if (renderers != null) {
                        count += renderers.Count;
                    }

                    if (noShadowRenderers != null) {
                        count += noShadowRenderers.Count;
                    }

                    if (shadowOnlyRenderers != null) {
                        count += shadowOnlyRenderers.Count;
                    }

                    return count;

                } else {

                    return renderer != null ? 1 : 0;

                }

            }
        }

    }

}
