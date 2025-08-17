Templates allow entities to be grouped and to be given additional behaviors such as behaving and appering like cassette blocks or dream blocks, moving like zipmovers or falling blocks and more! Any of these behaviors can be combined to make nearly any type of entity. 

To use templates, make a room titled zztemplates-[ROOMNAME] to store all your templated regions (This is not the room that the player enters - it is just a room to build the structures that get pasted in to the rest of the map). Mark a region to be used as a template with a 'template filler'. Everything in these fillers gets added to the template - entities, triggers, decals and tiles. In the template's name parameter, put some [TEMPLATENAME]. It's encouraged to add a node to the filler to act as the 'origin' of the entity group. If you don't add one, it defaults to the filler's top left corner. Finally, to use this region, simply use [ROOMNAME]/[TEMPLATENAME] as the template parameter in any of the template entities! These templates can be in the gameplay room or can be inside the template room to become part of larger templates themselves.

 Most vanilla entities and modded entities are 'supported', but not all of them will properly work in all cases since the template system makes assumptions about how classes are implemented. All templates have a 'depthoffset' parameter which is added to the depth of all child entities. This is useful to get the layering order you want. Templates can be categorized in broad types, which we will individually cover.

# Curve following templates
These templates follow paths, either curved or straight. Paths have segments, with each segment going between 'stopping points'. They're built using nodes. There are two types of nodes - knots and regular. Knots are made by placing two nodes directly on top of eachother. These knots are the 'stopping points' that the segments run between. Further detail can be added using singlular nodes The 'spline' option allows a choice of spline type. The current options are
 - simpleLinear: This makes all nodes into knots. This is the most analogous to existing entities.
 - compoundLinear: Connects nodes within a segment with straight lines. Makes jagged paths.
 - centripetal/uniform Normalized/Denormalized: Varieties of curved splines. Each segment will smoothly run through the nodes and will sharply turn at knots. 'centripetal' and 'uniform' change how the spline is shaped (centripetal is generally more stable). Normalized splines move at a constant speed and denormalized splines move 'faster' where the distance between nodes is larger. It's reccomended to try playing around with these to find the mode you like.

Spline using entities have a 'Last Node is Knot' checkbox, ticked by default. When making loops, however, it may be desirable to uncheck this to smoothly define the lasat segment of the loop (or provide one continuous smooth shape in the case of the curved splines). Further details are specific to each entitiy:   

### Template Zipmover
When the zipmover is triggered from rest, it travels to the next knot of the spline.  
 - return type: How the zipmover should return
   - none: The zipmover will move down its path once, getting stuck at the end
   - loop: The zipmover can be triggered from the last node and will travel to the first
   - normal: The zipmover travels backwards through the knots once it reaches the end. Vanilla behavior.
 - activation type: How to activate the zipmover.
   - ride: normal behavior
   - dash: activate by dashing into a solid part of the template
   - ride/dash automatic: require the player to activate at the first node but move through all remaining nodes automatically (a la starlight station)
   - manual: the zipmover will only be triggered by triggering events. See the template trigger modifier for more details!

You may want to make grouped zipmovers. This can be done by specifying a channel - zipmovers on the same channel will move together.

### Template Belt
Creates a string of templates that move along a path like a conveyer belt. If loop is not selected, new instances will be created on the initial end and be destroyed at the far end.
 - speed: How far each item on the belt will travel along the segment in a second. The amount of time to travel from one knot to the next is 1/speed regardless of how far appart they are.
 - num per segment: The number of instances that will be on the belt per segment
 - initial offset: the offset of the first instance on the belt (from here, all further instances will have even spacing). Offsets are proportional, ranging from 0 to 1.

### Template Channelmover
Moves to the knot matching the channel. If it has three knots and the channel it is on changes from zero to five, it will move forward by 5 segments, wrapping around once and ending on knot 2. 
 - asym: an asymmetry parameter for speed. Moving in the negative direction has its speed multiplied by this amount.
 - easing: the easing to be applied when moving along each segment.
 - complete: the channelmover won't switch its direction until it reaches a knot. Changes behavior only when the channel changes while the mover is in motion.
 - alternate easing: Easing applies the same in both directions of movement

### Template Swapblock
Dashing will move the block to the next node. You can specify custom speeds. Does not automatically return by default, though the behavior can be enabled.

# Collinding Templates
Colliding templates move through the level and are stopped when a solid they contain hits a solid or jumpthru in the level (like vanilla fallingblocks or move blocks). There are various options they all share:
 - Hit Jumpthrus: Whether jumpthrus will stop the template's motion. (Only stops templates going in the opposite direction of the jumpthru. A left-facing jumpthru from maddie's helping hand will only stop left-moving colliding templates - the same behavior as it exists for players)
 - throughDashblocks: Whether the entity will go through dashblocks and tempalte blocks
 - leniency: The amount of cornercorrection these entities will use when blocked by a small amount
 All entities here are highly configurable with physics options, custom speeds and swappable sound effects

### Template Pushblock
Like the entity from sj's frosted fragments, dashing into this entity will push it in the direction of the dash. 
 - no physics time: the grace time in which gravity does not apply after being hit. Specifying more values (comma sepperated) will apply them to various physics moments. In order: Dash, block beneath removed, template hitting a spring, hitting a spring on the template
 - Horizontal Drag: the drag applied horizontally. By default, the drag on floors is twice as high as midair. Specifying two values (comma seperated) will use the second value as the floor drag.
 - alwaysDrag: apply horizontal drag even in no physics time
There are many other customizable physics settings but they are all intuitive.

### Template Falling Block
A falling block. Custom falling directions can be specified.
 - Trigger channel: When this channel changes, the block will get triggered
 - Set trigger channel: Set the trigger channel above to 1 when starting to fall. Can be used to make grouped falling blocks that are triggered together.
 - triggered by riding: the default behavior. When unchecked, can only be triggered by triggering events (see trigger modifier)
 - Reverse channel: Reverses the direction the template falls when set.

### Template Moveblock
A moveblock. Placing a node adds an arrow decal that indicates the direction of movement. There is an option to make these steerable
 - uncollidable blocks: Uncollidable solids in the template (such as nonpresent cassette blocks) won't stop the block from moving through stuff

### Template Kevin
Works like kevin. The directions he can be hit from can be specified and an option for no auto returning is available. Speed can also be customized.

# Template Holdable
Holdable container esque template. The collider size exactly matches the size in loenn. If a node is used, will align with the node origin in the template filler. Many of the fields are self-explanatory. The less obvious ones are
 - Always Collidable: Keep solids in the template collidable even while being held by Madeline. Do not put solids on top of where Madeline would be if using this.
 - Holdable collider expand: Expand the holdable collider (the zone that can be used for pickups) outwards by this amount. Giving just one value will expand all sides by the same amount. Giving two (comma seperated) will expand the top and bottom by the first and the sides by the second. Three specify the top, sides and bottom in order and 4 dictates each side in clockwise order.
 - Player/Holdable momentum weight: How to determine speed after grabbing. Holdable weight of 1 and player weight of 1 will add together both velocities for final speed. Weights can be above one (to the joy of a certain mapper ;shivers;)

# Modifier templates
These templates modify the behavior/interactions of entities inside them

### Material template
For use with material layers. See wiki on the material controller

### Template Dreamblock Modifier
Turns a template into a big dreamblock. Entities work normally if the player is not dashing but if they are, anything in this template can be dreamdashed through without normal collision. Moving entities in this template will give the player liftspeed when traveling through.
 - Trigger on enter: Trigger parents when the player enters the dreamblock from a non-dreamdash state
 - Trigger on leave: Trigger parents when the player ends the dream dash state when exiting this template

### Template Trigger Modifier
Some templates have a notion of being 'triggered'. For example, falling blocks fall when triggered, zipmovers move when triggered and template blocks can be configured to crumble when triggered. What counts as 'triggering' can be customized with the trigger modifier.
 - Delay: All triggering events from children are delayed by this amount before being sent to parents 
 - Trigger on touch: Trigger the parents on all touch events. These include the normals of riding or climbing, but this will also include things that typically wouldn't affect blocks such as buffered climbjumps or wallbounces.
 - Advanced touch options: Filters the afforementioned touch events that count as triggering. Serves as a whitelist when trigger on touch is not selected and a blacklist when it is. (For example, chose 'wallbounce' with trigger on touch disabled to make a block that only triggers when wallbounced on)
 - Channel: Create a triggering event when this channel changes
 - Set channel: When recieving a triggering event, set the given channel
 - block trigger: Triggering events are blocked
 - block filter: A list of triggering events that have the opposite behavior to block trigger
 - log: Used to get the name of trigger events to write block filter. Trigger events ending with a star act as wildcards. For example, "hit/*" will match all hit triggering events.
 - seekers trigger: Seekers will emit a triggering event when hitting entities in this template
 - Holdables trigger: hitting this template with a holdable will trigger it

# Misc
### Template Block
A generic template for pasting in something that doesn't need to move/disappear/be attached to stuff
 - Exit block behavior: Whether to make this block an exit block (will be invisible if the player starts inside it and will fade in once the player leaves)
 - Triggerable: Whether this should break on trigger events (see trigger modifier)
 - Trigger on break: Whether breaking this should trigger parent templates
 - Breakable by blocks: Whether colling templates will break this block
 - Visible: Whether the things in the template are visible
 - Collidable: Whether they can be collided with
 - Active: Whether entities inside should update
 - Can break: whether the player's dash can break this block
 - Only redbubble...: like the temple blocks you need a redbubble for yaknow?
 - Persistent: if the block has been broken, don't make it again when the room is reloaded
 - Propagate riding: Will let parent templates know if a child entity is being ridden
 - Propagate shaking: Shake if the parent template is shaking.

### Template Moonblock
A moonblock. Can configure the drift amount and sensitivity to dashes

### Template Staticmover
Attach a template to a moving solid
 - Liftspeed smear: Due to the way blocks move in celeste, the amount blocks move in any given frame is inconsistent when not moving at a multiple of 60 speed. This specifies how long liftspeed should be 'smeared' over to provide more consistency.
 - Smear average: The default behavior is to take the maximum liftspeed over the smeared frames - this will use average instead.
 - Channel: The cassette layer to use as visuals when the staticmover is disabled (from block disappearing). See more information in cassette section. If this is left blank, the template will be destroyed/reconstructed whenever the staticmover is disabled or enabled. Put any value into this field to disable this behavior.

 ### Template Fake Wall
Fake wall; disappears when you enter it. (It's supposed to be persistent but like I accidentally deleted that line right before last release)
 - Freeze: suppress all entity updates that are part of the wall
 - dontOnTransitionInto: won't appear at all if you transition into this entity
 - Persistent - whether to stop loading this entity once it is uncovered once. Saved in session (i.e. restarting chapter resets this)
 - Disappear Depth - depth of the template as it disappears

# The Cassette Section
### Template Cassette Block
Turn template into cassette
 - channel: which channel to make the entities solid on. Also controls which material group this entity is a part of. If no material group exists on the specified channel, the template will simply become invisible when its channel is not activated. See advanced cassette section for more details. 
 - freeze: whether to stop entities from updating when not solid

### Templalte Cassette Block Manager (simple)
Syncs to the vanilla cassette rhythm and is used to apply the visual effect to cassette templates.
 - Channel 1/2/3/4: channels to set when the corresponding cassette blocks should be visible. This also marks the channels that get used for visuals. Using 'channelXYZ' in channel 1 will set all template cassette blocks with the channel 'channelXYZ' to follow the state of the blue cassette blocks (and 2 for purple, 3 for yellow and 4 for green). This also will add templates with this channel to the proper coloring group.
 - Visual only: Don't change the channels - only add templates with the corresponding channels to the visual groups
 - no visuals: Only set the channels - don't apply the cassette effects
 - tintActive: tint the cassettes in the present state to the corresponding block colors (by default, visuals will only be applied to make the 'cassette shadow' when entities are gone)
 - translucent: Make the cassette shadows translucent
 - Simple style: use a visually simpler appearance

### Advanced Template Cassette block manage
Manages visuals for template cassette blocks and can also be used to set up a rhythm. The 'timings' part of this entity were written when I did not have proper knowledge of how cassette music works and is liable to be changed. Only use this entity for the visuals system.
 - materials: Specifies how to render things when they are not solid. Can be left blank to make entities completely invisible. Takes the form of [CHANNEL]:{param1:...,param2:...} where [CHANNEL] is the channel used by the templates (including verbatim modifiers! x will not render in a group with x[+0]) and the usable params are
    - border: hexcolor of the border
    - innerlow/high: these values will be interpolated between based on the color the entity would have at the given position. For instance, innerlow:#0008, innerhigh:#4448 would cause a dimmer version of the original entities to be present when not solid
    - x/y/time/phase: coefficients for sine function for diagonal stripes. x:0,y:1 creates horizontal stripes, x:1,y:0 does vertical and x:0.7,y:0.3 would do diagonal. Use smaller values to have wider stripes. w and h can also be used instead of x and y to specify the pixel width and height of the stripes
    - stripecutoff: cutoff of sine function to use for stripes. Between -1 and 1. Higher values result in skinnier stripes.
    - Depth at which to render the cassette shadow
    - fg: Setting this will cause this group to have a tinted appearance. The effect will render at the depth specified as the value
    - fghigh/fglow: The color of an entity in a tinted group is set between these two values based on its brightness
    - fgsat: How much of the entity's color to mix in to the above color. Between 0 and 1.

# Entity ID Marker / Template Gluable
Entity markers mark an entity as a target for the template gluable. This does not work for all entities but does work for many. Uses the loenn entity ID as the parameter 'path'. With templates, it becomes possible to have many entities from the same id. When using this entity to mark items in templates, use the entire path of the template's entityid's. Say you are placing a template cassette block containing a template zipmover containing a jelly. You should use the path [ID of template cassette block]/[ID of template swapblock]/[Id of jelly]. This id is also used in the reflection functions of the math controller text compiler. Markers should be placed in a zztemplates room.

Once a marker is made, using a template gluable with the identifier will attach a template to that entity. 