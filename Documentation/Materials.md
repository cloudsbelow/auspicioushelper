Load shader effects and place them at various depths! You can pass entities into these effects using material templates, allowing shader effects to be masked to specific entities. The passes you specify will be applied in succession. Material templates will be drawn in using the first effect. Then, this result will be drawn using the next effect and so on untill all passes are applied.
 - **Identifier**: an identifier for this shader. ***If the identifier is the same, auspicioushelper assumes all other fields are the same.*** Leave blank to automatically use a identifier unique to the passes, texture and params. Effects will be kept at full strength when transitioning if a materialcontroller with the same identifier is present both before and after. This identifier is also used when passing the output of this layer to other layers
 - **Passes**: A comma sepperated list of effect passes to apply, rooted in the Effects folder. The first pass can be 'null', which will cause the material templates to get drawn to the first texture with no effect applied. This is useful when the affect you want to apply relies on surrounding pixel information; more information is provided for effect writers below. 
 - **Textures**: A k/v list of texture slots and the resource to pass to them. There are two special values: *bg* and *gp*. *bg* will pass in the background (i.e. background stylegrounds) and *gp* will pass in the current gameplay buffer, which will contain entities with a higher depth than the layer. You can also pass in the result of a layer by prepending a dollar sign. Since layers are drawn in order, if using a layer with lower depth as an input, the previous frame's value is used. Example:
   > `1:bg, 2:$maskTemplates` will put the background in texture slot 0 and the result of the maskTemplates layer in slot 2. 
 - **params**: uniform parameters to pass to the shader as a list of k/v pairs. Can pass bools, colors, ints, floats and floatarrays as well as dynamic channel values. An example parameter set may appear as "color:#fff, strength:@strchannel*0.3, offset: 0.15, position:[1,1]"
   - Colors declared as hex values are passed as float4
   - float/int type is determined by the existence of a decimal point in the provided thing
   - Channels must be prepended with an @ and can be amended by a multiplicative factor to use (since channels take integer values and we pass uniform floats to the shader). To pass one tenth the value of a channel *numjumps* you would use *@numjumps/10* as your parameter value.
   - Lists specified as *[n1, n2, n3]* are passed as contiguous floats to the shader. If filling a float3 value, there should be 3, for instance, and if filling a float2[5] or float[10] there should be 10. Just use what makes sense.
 Monogame hates me so sometimes these can be weird. Example:
   > `sigma:@blurStrength, alpha:0.3`
 - **Depth**: at what depth to render this effect
 - **Fade in/out**: How to fade in/out the effect when moving from a room without it to a room with or vice versa.
   - *Always*: Keep this effect at full strength for the entire transition
   - *Never*: Do not show this effect for the entire transition
   - *Linear/Cosine/Sqrt*: different easing functions
 - **Always**: Whether to draw this effect even if no material templates in the scene use it. Useful for ambiance effects like smoke.
 - **Quad first**: Whether to draw a quad to the screen in the first pass, letting the first fragment shader run for every pixel (otherwise, the effect is only run where entities from material templates are drawn). Commonly used with the 'always' option.
 - **Draw in scene**: Whether this effect should be drawn to scene. If left unchecked, can be used as an input to other material layers.
 - **Reload**: Whether to reload the effect when the entity is constructed. Can be useful if you are coding an effect and want to test changes without relaunching the game. The default behavior is to cache effects.

# For shader writers

A few uniform values will be filled in by default.
 - float *quiet*: whether everest photosensitive mode is on (1 if true) 
 - float2 *cpos*: the world position of the camera (upper left corner)
 - float2 *pscale*: the current size of gameplay pixels in texture space. This will be the default (1/320, 1/180) most of the time but when using extended camera dynamics to zoom out, textures may be resized. To make your effects compatible with extended camera dynamics, sampling at some pixel offset *float2 offset* from a given position should be done by sampling the texture at *position+offset\*pscale*
 - float *time*: the amount of time the scene has been active in seconds. Starts at 2 for various reasons.

For the second pass and beyond, texture slot 0 contains the result of the previous pass and the higher textures contain any user specified ones. This means, since all targets are the same size, HLSL's TEXCOORD0 builtin will match the screenspace texture position of the result. The first pass is unique and is where the sprites are directly rendered. Slot 0 will contain the spritebank texture sheet and rather than screenspace, TEXCOORD0 will specify the UV coordinates as relevant on the sheet. Since screenspace effects are much harder to do in this first pass due to these restrictions, it is reccomended to make the first pass 'null' to use the default entity rendering. 

 There is an option in auspicioushelper to 'use quiet shaders' for additional optional accessability. This is distinct from the 'quiet' uniform. When selected, if using effect 'exampleMod/shader.fx', auspicioushelper will attempt to load and use 'exampleMod/shader_quiet.fx'. If a quiet shader is not provided, auspicioushelper will continue using the original. 