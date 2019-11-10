using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace Bearroll {

    public partial class GDOC_Base : MonoBehaviour {

        CommandBuffer cbAfterDepth;
        CommandBuffer cbAfterEverything;

        [DllImport("GDOC")]
        static extern IntPtr GetRenderEventFunc();

        public bool isSRP {
            get {
                #if UNITY_2018_1_OR_NEWER
                return GraphicsSettings.renderPipelineAsset != null;
                #else
                return false;
                #endif
            }
        }

        void OnPreCull() {
            BeforeEverything();
        }

        void RemoveBuffer(Camera camera) {

			if (cbAfterDepth == null) return;

            camera.RemoveCommandBuffer(CameraEvent.AfterGBuffer, cbAfterDepth);
            camera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, cbAfterDepth);
            camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, cbAfterEverything);

        }

        CommandBuffer GetAfterDepthBuffer() {

            if(cbAfterDepth == null) {

                cbAfterDepth = new CommandBuffer();
                cbAfterDepth.name = "GDOC_AfterDepth";

                cbAfterDepth.IssuePluginEvent(GetRenderEventFunc(), 0);

            }

            return cbAfterDepth;

        }

        CommandBuffer GetAfterEverythingBuffer() {

            if(cbAfterEverything == null) {

                cbAfterEverything = new CommandBuffer();
                cbAfterEverything.name = "GDOC_AfterEverything";

                cbAfterEverything.IssuePluginEvent(GetRenderEventFunc(), 3);

            }

            return cbAfterEverything;

        }

        protected void Init(Camera camera) {

            GetAfterDepthBuffer();
            GetAfterEverythingBuffer();

            if(camera.actualRenderingPath == RenderingPath.DeferredLighting || camera.actualRenderingPath == RenderingPath.DeferredShading) {
                camera.AddCommandBuffer(CameraEvent.AfterGBuffer, cbAfterDepth);
            } else {
                camera.depthTextureMode |= DepthTextureMode.Depth;
                camera.AddCommandBuffer(CameraEvent.AfterDepthTexture, cbAfterDepth);
            }

            camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, cbAfterEverything);

        }

        protected void Clean(Camera camera) {

			RemoveBuffer(camera);

		}

        public virtual void BeforeEverything() {

        }

    }
}
