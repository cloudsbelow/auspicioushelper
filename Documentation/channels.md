Channels store integer data on string keys - they're a lot like flags and counters but they revert their state to the original from entering the room on death (like coremode). It is recommended to use channel names without white space or special characters other than underscores such as 1_fish or x (for the regex enjoyers, they should match /\w+/). Channels are very performant and are used instead of flags in most auspicioushelper text entires. 

### Access modifiers:
Sometimes you might want to invert the value of a channel for a certain entity (or some similarly simple operation) without the use of a Math Controller. Access modifiers can accomplish this! Simply decorate a channel with some operations in brackets. A simple example is inverting the result of some channel x by doing x[\!], which will only be 1 when x is 0. You can chain together modifiers with commas. A more complicated example would be x[+1,%3,==2] which will be tied to the value (x+1)%3==2. In addition to the basic arithmetic operations (+, -, *, /, %, ==, >, <, >=, <=, !) and bitwise operations (~, ^, &, |, <<, >>), we also support max and min which work as expected and x/, x>>, x<< which perform the corresponding operations with orders reversed (so, somewhat unintuitively, ch[x/3] has value 3/ch). Finally, we support 'safe modulo' %s which is in the positive interval even for negative inputs. Note that for entities that set channels, using modifiers will prevent them from setting a new value.

### Flag and counter interoperability
Yes! Most mods use flags and not channels. Don't panic though! If you are using entity that needs a channel but the value you want is a flag, you can use $*flagname* which will create a channel whose value matches the value of the flag with *flagname*. The same can be achieved for counters by using a '#' instead of a '$'. If you are using an entity that needs flags or coutners but want to use channels, you can do so with @*channelname* which does the opposite. Since channels are integers, any nonzero value counts as an activated flag. 

This can be combined with access modifiers to do some silly things. Let's say you want a flag that is true when some counter is a multiple of 5. You can do @#counter[\%5,==0]. What this does is first announces that you want to use a channel as a flag with the '@'. Then, the '#' notes that the channel is equal to some counter. Then, we check if counter\%5==0, checking divisibility by 5.

### Entities and Triggers
## Channel Player Trigger
Modifies a specified channel when the player does a certain action inside the trigger. These actions include entering and leaving the zone, dashing and jumping. You can choose the operation to perform on the player action (such as addition, xor, etc)

## Channel Clear Controller
Clear certain channels with a certain prefix when constructed and can also set the value of a single channel. Should be used if you want to prevent player from entering the room with undesired channel values. This entity *should not* be put into templates; doing so can cause ordering issues.

## Channel Math Controller
Can be used to run short arithmetic scripts on channels and read flags, entity statuses and more. Information on scripts can be found at the [text compiler](https://cloudsbelow.neocities.org/celestestuff/mathcompiler) or the visual [blockly compiler](https://cloudsbelow.neocities.org/celestestuff/visualmathcompiler). The provided script will run when some condition is met; scripts are coroutines but will run up to the first 'wait' clause on the frame they are activated.
 - **Compiled Operations** These come from either of the above compiler sites. This should appear as nonsense base64, i.e. unintelligible letters and numbers.
 - **Multi type**: How the controller should behave if asked to start a script when another is already running
  - BlockIfActive - The controller will not run again until the current instance runs to completion
  - ReplacePrevious - The controller will immediately abort the previous instance and start over with a new one
  - AttachedMultiple - Multiple scripts can be running concurrently and updates to channels will be visible in all running instacnes of the script
  - DetachedMultiple - Multiple scripts will be running but channel values in the scripts will be locked to what they were when it started running
 - **Activation Cond**: the conditions upon which to run the script
  - Interval - will try to run every x frames where x is the custom polling rate
  - OnChange - will try to run whenever a notifying channel changes (see notifying override)
  - Auto - Will be OnChange unless a custom polling rate is specified, in which case will be Interval
  - IntervalOrChange - will try to run whenever either a notifying channel changes or an interval passes
  - IntervalAndChange - will only try to run when there has been some change to notifying channels and the interval has passed
  - OnlyAwake - will only run when the scene is started
 - **run when awake**: run when added to the scene. Automatically set if 'onlyAwake' chosen.
 - **run immediately**: run the instant a notifying channel is changed. Potential for infinite loops; use with care.
 - **only run for nonzero**: Ignore changes to notifying channels unless they are being set to a nonzero value.
 - **notifying override**: By default, changing any channel used in the mathcontroller results in a 'change' that is picked up by OnChange controllers. Entering channels here (comma sepperated) will disable this default behavior and instead run the script when any of the notifying override channels changes.
 - **debug**: If checked, logs some info to the auspicioushelper debug console

## Channel Math Trigger
Trigger variation of the above entity. 

## Channel Switch
A core switch that changes a channel when a player (or other actor) hits it. If the switch is only on or only off, if the channel isn't set to the value that the switch will change the channel to, it is available to be switched (this is a confusing, verbose way to say that stuff will behave as you would expect it to). However, if neither is checked and the channel value does not match either on/pff value, the orientation it has is somewhat arbitrary.
 - on/off only: Whether the switch should only be usable in the on/off positions respectively
 - player/throwable/seeker toggle: Which entities can flip the switch
 - on/off value: the values to change the channel to
 - cooldown: How long until the switch can be used again

## Channel Sprite
An entity for displaying animations based on a channel. Animation states are read from Sprites.xml and work just like xmls for other entities. The animation played will be the one specified in "case[XYZ]" where [XYZ] is the current value of the channel. An example can be found in auspicioushelper/Graphics/Sprites.xml [here](/cloudsbelow/auspicioushelper/blob/main/Graphics/Sprites.xml)
 - Attached: Whether to try to attach this entity to moving blocks. Looks for blocks at exact entity position.
 - Edge type: Whether to loop through cases or clamp when value is greater than the number of provided animation states
 - offset X/Y: How much to offset the sprite from the entity position
 - xml spritename: The name of the sprite to use in Sprites.xml.
 - cases: The number of animation states provided in Sprites.xml

## Channelmover / Template Channelmover
Move between two node positions based on the least significant bit of the channel (original position of channel is even, node position if odd). Immediately changes direction when channel changes. Non-template channelmovers are obsolete.
 - Move time: the duration in seconds of the movement
 - Asym: the relative (asymmetric) speed of the movement of the outgoing block to the returning one

## Channelblock / Template Cassette Block
Appear based on the provided channel. Channelblocks are obsolete. See Template Cassette section for more information.

## Channel Booster
Booster that can be missing/normal/reverse based on a channel. Self-activating will change the channel every time you boost from it.

## Channel Jelly
Jellyfish that can be made inactive/into a platform/into an ice core block based on a channel value

## Channel Theo
Wymiki made me do this. I am not to be blamed for the horrors of this entity.
