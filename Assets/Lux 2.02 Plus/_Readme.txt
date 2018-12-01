Hi there and welcome to Lux Plus.

Before you can use Lux you will have set up your project properly.

As you most likely use deferred rendering you have to assign the “Lux Internal-DeferredShading” shader: Go to “Edit” -> “Project Settings” -> “Graphics” -> “Built in shader settings” -> “Deferred” and set it to “Custom”.
Then assign the “Lux Internal-DeferredShading” shader to the new slot (the shader is located in “Lux Shaders/Lux Core/Resources”).

You also have to assign the custom deferred reflection shader “Lux Plus Internal-DeferredReflections” and the custom “Lux Plus DepthNormal” shader.
You will find these in the same folder as the deferred lighting shader.

Please make sure that your project uses linear color space (“Edit” -> “Project Settings” -> “Player” -> “Other Settings” -> “Color space”) and your camera is set to “HDR”.
