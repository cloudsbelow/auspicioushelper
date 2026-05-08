Channels are similar to flags, but can store any numerical data. They reset when you die and save on room transition. For simplicity, you should avoid channels with special characters. Try sticking to letters, digits and underscores. Auspicious uses channels for everything for better performance, but if you are used to working with flags, they can be used as well.

## Channel Expressions:
You can do inline logic on channels very! Just put any math you want inside of parenthesis. For instance, (!(x+y) || z==0) will be true if the sum of x and y is zero or if z is equal to 0. Booleans here are numerically represented by 0/1. 

You can use flags, counters and sliders as channels as well. A channel starting with a dollarsign *$flagname* will have the value of 
the flag *flagname*. You can similarly use # for counters and ? for sliders.

If you have a channel that you want to use as a flag, channel or slider, you can similarly use *@channelname*. Some entities don't allow channel expressions to be used as flags. Mapping utils gives a helpful GUI to debug all your channel values and set them to whatever you want in order to debug these (and similar) cases. Here are some examples:

```
// A channel that inverts the value of flag 'cat'
(!$cat)

// A channel that does some math on another
(x*2 + 10)

// A channel that is true if all conditions are met
(numDashes<2 && $hasBerry && max(progress1,progress2)==3)

// A flag based on channel values
@(backtracking && (cam1 || cam2))

// A channel checking some slider value
(floor(?slider) == floor(segment))

// A channel that is always false
(0)

// A channel that is always 10
(10)
```
### Legacy note
Operations can also be performed on channels by appending modifiers in brackets. The following are equivalent:
``` 
- (!$flag)            $flag[!]
- (floor((y+3)*5)+3)  y[+3, *5, floor, +3]
- (x && y && (z||w))  and(x,y,or(z,w))
```
These operations will remain supported but are less flexible than the newer system.

## CHANNELABLE fields
Most numerical fields in auspicious, such as falling block speed or holdable throw strength, are 'Channelable' meaning that, in addition to taking numbers, also accept channels. When these channels change, those values will change to match. This can be nice for making adjustable speed belts or similar and is recommended over entity modifier template time manipulation for better liftspeed calculations.

## 'Advanced' setters
Some entities, such as the channel player trigger and template trigger modifier, can set channels in an advanced way. Channels and their new values can be given in a list with optional keys after ':'. For example
```
// Set x and y to 1
x,y

// Set x to 10 and invert y
x:10, y:y[!]

// Set z to the sum of x and y
// This increases x by y every time
// the setter is triggered
x:(x+y)
```

## Entities and Triggers
### Channel Approach Controller
Make one channel value gradually approach another channel value with a speed specified by a third. Can be used for fades, easing and similar.

### Channel Player Trigger
Modifies a specified channel when the player does a certain action inside the trigger. These actions include entering and leaving the zone, dashing and jumping. You can choose the operation to perform on the player action (such as addition, xor, etc)

### Channel Clear Controller
Clear certain channels with a certain prefix when constructed and can also set the value of a single channel. Should be used if you want to prevent player from entering the room with undesired channel values. This entity *should not* be put into templates; doing so can cause ordering issues.

### Channel Math Controller
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

### Channel Math Trigger
Trigger variation of the above entity. 

### Channel Switch
A core switch that changes a channel when a player (or other actor) hits it. If the switch is only on or only off, if the channel isn't set to the value that the switch will change the channel to, it is available to be switched (this is a confusing, verbose way to say that stuff will behave as you would expect it to). However, if neither is checked and the channel value does not match either on/pff value, the orientation it has is somewhat arbitrary.
 - **on/off only**: Whether the switch should only be usable in the on/off positions respectively
 - **player/throwable/seeker toggle**: Which entities can flip the switch
 - **on/off value**: the values to change the channel to
 - **cooldown**: How long until the switch can be used again

### Channel Sprite
An entity for displaying animations based on a channel. Animation states are read from Sprites.xml and work just like xmls for other entities. The animation played will be the one specified in "case[XYZ]" where [XYZ] is the current value of the channel. An example can be found in auspicioushelper/Graphics/Sprites.xml [here](/cloudsbelow/auspicioushelper/blob/main/Graphics/Sprites.xml)
 - **Attached**: Whether to try to attach this entity to moving blocks. Looks for blocks at exact entity position.
 - **Edge type**: Whether to loop through cases or clamp when value is greater than the number of provided animation states
 - **offset X/Y**: How much to offset the sprite from the entity position
 - **xml spritename**: The name of the sprite to use in Sprites.xml.
 - **cases**: The number of animation states provided in Sprites.xml

### Channelmover / Template Channelmover
Move between two node positions based on the least significant bit of the channel (original position of channel is even, node position if odd). Immediately changes direction when channel changes. Non-template channelmovers are obsolete.
 - **Move time**: the duration in seconds of the movement
 - **Asym**: the relative (asymmetric) speed of the movement of the outgoing block to the returning one

### Channelblock / Template Cassette Block
Appear based on the provided channel. Channelblocks are obsolete. See Template Cassette section for more information.

### Channel Booster
Booster that can be missing/normal/reverse based on a channel. Self-activating will change the channel every time you boost from it.

### Channel Jelly
Jellyfish that can be made inactive/into a platform/into an ice core block based on a channel value

### Channel Theo
Wymiki made me do this. I am not to be blamed for the horrors of this entity.
