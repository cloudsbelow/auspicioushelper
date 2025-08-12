Templates allow you to include sections of a room as an entity in your maps! Using them, you can make zipmovers or swapblocks with custom shapes. To use these, make a room titled zztemplates-[ROOMNAME] to store all your templated regions. Then, place a template filler over the region you want to use. For the 'template' parameter put some [TEMPLATENAME]. Then, to use your template, simply use [ROOMNAME]/[TEMPLATENAME] as the template parameter! The node of a template filler will be used as the origin of the section when pasting it into other rooms. Template sections can also include other templates! This can be used to create very complex entities (like cassette swapblock refill crystals) (why would you do that?). Most vanilla entities and modded entities are 'supported', but not all of them will properly work in all cases since the template system makes assumptions about how classes are implemented. (like how dare entities set their own visibility). Templates also work automatically with decals!

All templates have a 'template' parameter as mentioned above. They also have a depthoffset parameter - all entities (and tiles and decals) will have their depts adjusted by this amount.

### Template Zipmover
A zipmover that supports multiple stops and crooked paths. Nodes specify the zipmover path. Place two nodes on top of eachother to indicate a 'knot' that the zipmover should stop at (these are like red nodes in osu if you have experience mapping sliders).  
 - return type: How the zipmover should return
   - none: The zipmover will move down its path once then remain there
   - loop: The zipmover will loop from its last node back to the first
   - normal: default behavior
 - activation type: How to activate the zipmover.
   - ride: normal behavior
   - dash: activate by dashing into the template's tiles
   - ride/dash automatic: require the player to activate at the first node but move through all remaining nodes automatically (a la starlight station)
 - last node is knot: whether to make the last node always be a knot (only uncheck this if you are making a looping zipmover and want a crooked path on the last segment.

### Template Swapblock
A swapblock that supports multiple nodes.

### Template Staticmover
Attach a template to a moving solid
 - Liftspeed smear: Due to the way blocks move in celeste, the amount blocks move in any given frame is inconsistent when not moving at a multiple of 60 speed. This specifies how long liftspeed should be 'smeared' over to provide more consistency.
 - Smear average: The default behavior is to take the maximum liftspeed over the smeared frames - this will use average instead.
 - Channel: The cassette layer to use as visuals when the staticmover is disabled (from block disappearing). See more information in cassette section. If this is left blank, the template will be destroyed/reconstructed whenever the staticmover is disabled or enabled. Put any value into this field to disable this behavior.

### Template Channelmover
Moves between two nodes based on a channel. Can be given different easings.

### Template Holdable
Holdable container esque template. The collider size exactly matches the size in loenn. If a node is used, will align with the node origin in the template filler. Many of the fields are self-explanatory. The less obvious ones are
 - Always Collidable: Keep solids in the template collidable even while being held by Madeline. Do not put solids on top of where Madeline would be if using this.
 - Holdable collider expand: Expand the holdable collider (the zone that can be used for pickups) outwards in all directions by this amount.
 - Player/Holdable momentum weight: How to determine speed after grabbing. Holdable weight of 1 and player weight of 1 will add together both velocities for final speed. Weights can be above one (to the joy of a certain mapper ;shivers;)

### Template Fake Wall
Fake wall; disappears when you enter it. (It's supposed to be persistent but like I accidentally deleted that line right before last release)
 - Freeze: suppress all entity updates that are part of the wall

### Template Block
A generic template for pasting in something that doesn't need to move/disappear/be attached to stuff
 - Visible: Whether the things in the template are visible
 - Collidable: Whether they can be collided with
 - Active: Whether entities inside should update
 - Can break: turns the block into a dash block
 - Only redbubble...: like the temple blocks you need a redbubble for yaknow?
 - Persistent: if the block has been broken, don't make it again when the room is reloaded

### Template Cassette Block
Turn template into cassette
 - channel: which channel to make the entities solid on. Also controls which material group this entity is a part of. See manager section.
 - freeze: whether to stop entities from updating when not solid

### Templalte Cassette Block Manager
Manages visuals for template cassette blocks and can also be used to set up a rhythm. This is a very non-trivial entity. Sorry. It is recommended to look at auspicioushelper test map for clarification if confused. We use timeevents to refer to a list of things to do at a certain time. They are specified as a k/v pair like "sound:[SOUNDEVENT],channels:{x:1,y:0}" (do not use quotes). The sound can either be big/small to use the default cassette clicks or an fmod event path. Channels specify a list of channels to set and the values to set them to.
 - timings: List of time/timeevent pairs. May appear like "0:{sound:big,ch:{x:1,y:0}},1:{sound:small},2:{sound:big,ch:{x:0,y:1}},..."
 - materials: Specifies how to render things when they are not solid. Can be left blank to make entities completely invisible. Takes the form of [CHANNEL]:{param1:...,param2:...} where [CHANNEL] is the channel used by the templates (including verbatim modifiers! x will not render in a group with x[+0]) and the usable params are
   - border: hexcolor of the border
   - innerlow/high: these values will be interpolated between based on the color the entity would have at the given position. For instance, innerlow:#0008, innerhigh:#4448 would cause a dimmer version of the original entities to be present when not solid
   - x/y/time/phase: coefficients for sine function for diagonal stripes. x:0,y:1 creates horizontal stripes, x:1,y:0 does vertical and x:0.7,y:0.3 would do diagonal. Use smaller values to have wider stripes.
   - stripecutoff: cutoff of sine function to use for stripes. Between -1 and 1. Higher values result in skinnier stripes.
 - channel/useChannel: If selected, will only activate the timing loop when a certain channel is active 
 - onactivate/deactivate: The timing event to use when the activation channel changes to active/inactive
 - beats per measure: As expected, specifies highest key for timing events used in timings section
 - beats per minute: beats per minute
 - correct: If active, will run through timing events when reactivated to correctly set channel state
 - trysync/offset: to be implemented later, whether to try to sync to music

### Entity ID Marker
To mark an entity for later use in channel math controller reflection functions. Uses the loenn entity ID as the parameter 'path'. This does not work for all entities but does work for many. With templates, it becomes possible to have many entities from the same id. When using this entity to mark items in templates, use the entire path of the template's entityid's. Say you are placing a template cassette block containing a template zipmover containing a jelly. You should use the path [ID of template cassette block]/[ID of template swapblock]/[Id of jelly]