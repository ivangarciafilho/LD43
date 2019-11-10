using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.SceneManagement;
using System.Diagnostics;

#if GDOC_LWRP

using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace Bearroll {
    
    public class GDOC_SRPass: ScriptableRenderPass {

        CommandBuffer cb;

        public GDOC_SRPass(CommandBuffer cb) {
            this.cb = cb;
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData) {
            context.ExecuteCommandBuffer(cb);
        }
    }

    public class GDOC_SRPass2: ScriptableRenderPass {

        CommandBuffer cb;

        public GDOC_SRPass2(CommandBuffer cb) {
            this.cb = cb;
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData) {
            context.ExecuteCommandBuffer(cb);
        }
    }

    public partial class GDOC_Base: IBeforeCameraRender, IAfterOpaquePass, IAfterTransparentPass {

        GDOC_SRPass pass;
        GDOC_SRPass2 pass2;

        public void ExecuteBeforeCameraRender(LightweightRenderPipeline pipelineInstance, ScriptableRenderContext context, Camera camera) {
            BeforeEverything();
        }

		ScriptableRenderPass IAfterTransparentPass.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle) {
			if (pass2 == null) {
				pass2 = new GDOC_SRPass2(GetAfterEverythingBuffer());
			}
			return pass2;
		}

		/*
		ScriptableRenderPass IAfterSkyboxPass.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle) {
            if (pass2 == null) {
                pass2 = new GDOC_SRPass2(GetAfterEverythingBuffer());
            }
            return pass2;
        }
		*/

		ScriptableRenderPass IAfterOpaquePass.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle, RenderTargetHandle depthAttachmentHandle) {
			if (pass == null) {
				pass = new GDOC_SRPass(GetAfterDepthBuffer());
			}

			return pass;
		}

	}
}

#endif