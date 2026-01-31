# Shaders with Auspicioushelper
Auspicioushelper's materials let you take a group of entities, styleground layers or images and apply effects to them. They can be placed in either styleground layer using an 'auspicioushelper material effect' or at any depth in the level via a 'material controller' entity. You specify a sequence of shader passes to apply and they are applied in sequence. Some shaders can have 'parameters' which can be numbers, colors, positions or more. They may also take in one or more auxillery textures to combine together, such as in masking, where the main texture is used to mask a second texture. Textures can be nearly anything, such as groups of stylegrounds, entities or even the outputs of other materials.

## Specifying passes
For your passes, you should give a list of shaders (rooted in the Effects/ folder) such as  `null, ausp/maskBy, ausp/invertColor` - these passes will be applied in sequence, each using the output of the previous as its input. Auspicioushelper ships with some helper shaders that can be composed in various ways to provide some basic utility. 

**null**<br>
Do nothing! This is useful to gather entities into a layer without changing them. The first pass is special, so most materials that use entities should use null as their first pass (unless a texture 00 is specified with quad first - we'll explain this later).

**ausp/maskBy**<br>
Use the input's alpha as a mask for texture #1

**ausp/maskedFrom**<br>
Use the contents of texture #1 as a mask for the input

**ausp/invertAlpha**<br>
invert the alpha of the input. Useful with maskBy to invert the masked area.

**ausp/tint**<br>
Tint the input by the provided parameters
 - **low** The darkest color (what black becomes)
 - **high** The brightest color (what white becomes)
 - **sat** How much of the original hues to keep
For example, setting low to black, high to white, and saturation to 0 will make a black and white image. Changing high to red will make the image 'black and red' instead.

**ausp/static**<br>
Turn the input into static
 - **low/high** the darkest and brightest colors of the static respectively

**ausp/innerBorder, ausp/outerBorder**<br>
Give a border to the inputs. The 'inner' variety replaces the outermost pixels and the 'outer' variety expands from the input when making the border.
 - **color** The color of the border
You can apply multiple outerBorders to make thicker borders

**ausp/blurH, ausp/blurV**<br>
blur the input (H)orizontally or (V)ertically
 - **sigma** the strength of the blur (CANNOT BE 0)
A sigma of 1 spreads things out by around 1 pixel - can be fractional. blurH and blurV can be combined to make a normal circular blur - just apply the passes one after another

**ausp/opacity**<br>
Change the opacity of the input multiplicativity by the **opacity** parameter

## Specifying Textures
Textures in shaders are assigned to 'slots'. You specify which textures you want on which slots in a list with the format `slotnumber:texture`. For example, `1:%sgs, 2:/some/other/texture`. Slot 0 is special - it holds the current input and assigning something to it won't do anything most of the time. The other slots, 1-15, are all available to be used. Each shader will use certain fixed slots. For example, the masking shaders expect the other texture to be in slot 1.

**Styleground groups**<br>
(No relation to groups in the styleground tab) You can add a styleground to a group by adding `%groupname` to the 'rooms' option. This makes a virtual room with the selected stylegrounds. You can then use `%groupname` as a texture in any slot. The styleground gathering doesn't distinguish by foreground/background - they will all be stacked together if you put fg/bg shaders in the same group.

**Other materials**<br>
You can use the final output of any other material as a shader via the identifier. Add a dollar sign in front - i.e. `$materialidentifier`

**Images**<br>
You can use any image in Graphics/ as a texture by using a `/` as the first character. For example, to pass a colorgrade image, use `/colorGrading/path/cg` to get the .png at that location (don't include the file extension)

**Defaults**
 - `last` uses the material's previous frame's result
 - `gp` use the gp buffer - only contains entities with depth greater than the material's depth
 - `bg` use all current background stylegrounds as a texture. Can be buggy if using auspicious material effects. In this case, virtual `%` rooms should be used

## Specifying parameters
Some textures (such as those from the example) take in parameters. Parameters, like textures, are specified as a list of key/value pairs. You can put numbers, values like 'true' and 'false', or colors depending on the type of parameter (these vary by shader)

**Numbers**<br>
Either put the raw number or use a channel by prepending the channel name with a `@`. For example, `opacity:@op` uses the value of the channel `op` as the opacity parameter. This also works with all usual channel modifiers, letting you use inputs like `sigma:@$flag[*3,+1]` with the blur shaders to increase blur when a flag is set.

**Colors**<br>
Either use a `#` to specify a color directly or use an array of 4 values (rgba). For example, `high:#fff, low:[1,@yellow,0,1]` will set 'high' to white and 'low' to red if the channel 'yellow' isn't set and yellow if it is set (most shaders assume colors range in decimal values from 0-1 rather than from 0-255).

**Other numerical lists**<br>
Your lists can have an arbitrary length. If a shader you're using takes an array of 8 `float2`'s or a 4x4 matrix, you can pass a length-16 array. (this is a bit extreme; you won't need to do this for any but the most agregious shaders, likely)

## Shader identifiers
Material controllers and shaders both let you specify an identifier for the material. ***If the identifier is the same, auspicioushelper assumes all other fields are the same.*** This can cause caching issues if you use the same identifier for different materials carelessly. Leave blank to automatically use a identifier unique to the passes, texture and params. The identifier (as mentioned in the textures section) can be used to use the material as a texture input in other materials. It is also how to add entities to a material. There are two ways to do this - via a 'material template' which includes any templated entities or via a 'material adder' which adds a list of entities with certain id's (these are the numbers shown at the top of the entity editing box in loenn) to the layer. The ID of the player is 'player', and 'fg'/'bg' are the ID for foreground/background tiles respectively. 

## Other parameters
**Depth**/**RenderOrder**: at what depth to render this effect. In stylegrounds, this only affects when the material is processed - it won't be drawn to the screen until the correct time during bg rendering.

**Fade in/out**: For material controllers How to fade in/out the effect when moving from a room without it to a room with or vice versa. Full opacity will be kept for the whole transition if identical material controllers exist in both rooms.
  - *Always*: Keep this effect at full strength for the entire transition
  - *Never*: Do not show this effect for the entire transition
  - *Linear/Cosine/Sqrt*: different easing functions

**Always**: Whether to draw this effect even if no material templates in the scene use it. For most uses, should always be on

## Chaining layers, quad first, and slot 0
Earlier in the texture section, we mentioned that texture slots 1-15 are free for you to use and that slot 0 is special. Slot 0 always holds the 'input' texture (i.e. the result of the previous pass) in passes after the first. The first pass, by default, only uses included entities as an input. Specifying a texture for slot `00` lets you make a custom input in the first pass. This is useful if you have a sequence of passes you want to apply that need to use different textures on the same slot or different parameter values with the same name - you'd perform the first portion of passes in one material then continue with the rest of the sequence in another by setting slot `00` to the layer identifier of the first half. (You must also check quad first to tell ausp to actually draw this texture by default). Ausp expects you to use slot `00` here rather than `0` to recognize this uniqueness. 

A nice application of this is drawing an arbitrary texture on the screen. For example, by setting `00:%someshadergroup` in a material controller and using a single 'null' pass, you can draw a group of stylegrounds at a custom depth (remember to check quadFirst too to tell it to draw this!). If you're applying fullscreen shader effects, quadFirst is also a way to draw a fullscreen rect on the first pass, even without specifying a custom `00` (don't worry if you don't know what this means). 

## Troubleshooting steps
Modifying shaders during development can be weird and cursed. Once you have a working setup, it's rare for things to stop working but oftentimes while you're still developing, FNA will use old cached values annoyingly. Firstly, if 'reload' isn't checked, select it. Auspicious may use the cached shader otherwise. Nextly, make sure you have 'null' before all your shader passes if the layer holds entities (unless you're doing something with the uniqueness of the first pass! Very cursed). Finally, try refreshing (f5) or hard refresing (ctrl+f5). HLSL kind of is a jank language that often just needs to have everest restart when you change stuff. Also, check the auspicioushelper log to make sure your shader is being correctly found. Getting the path wrong is a common mistake!

# Brief note for shader writers
This isn't important to use provided auspicioushelper materials and is only important if you want to write your own. The following assumes knowledge of HLSL. 

A few uniform values will be filled in for all shaders by default.
- float *quiet*: whether everest photosensitive mode is on (1 if true) 
- float2 *cpos*: the world position of the camera (upper left corner)
- float2 *pscale*: the current size of gameplay pixels in texture space. This will be the default (1/320, 1/180) most of the time but when using extended camera dynamics to zoom out, textures may be resized. To make your effects compatible with extended camera dynamics, sampling at some pixel offset *float2 offset* from a given position should be done by sampling the texture at *position+offset\*pscale*
- float *time*: the amount of time the scene has been active in seconds. Starts at 2 for various reasons.

For the second pass and beyond, texture slot 0 contains the result of the previous pass and the higher textures contain any user specified ones. The TEXCOORD0 builtin will match the screenspace texture position of the fragment pixel here. The first pass is unique and is where the sprites are directly rendered, so slot 0 will contain the spritebank texture sheet (or first) and TEXCOORD0 will specify the texture UV coordinates. It is reccomended to use a initial 'null' pass if designing screenspace effects for this reason.