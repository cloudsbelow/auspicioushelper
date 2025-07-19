local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local channelswitch = {}

channelswitch.name = "auspicioushelper/ChannelSwitch"
channelswitch.depth = 2000

channelswitch.placements = {
  {
    name = "Channel Switch",
    data = {
      channel = "",
      on_only=false,
      off_only=false,
      player_toggle=true,
      throwable_toggle=false,
      seeker_toggle=false,
      on_value=1,
      off_value=0,
      cooldown=1.0,
      onSfx="event:/game/09_core/switch_to_cold",
      offSfx="event:/game/09_core/switch_to_hot",
      dieSfx="event:/game/09_core/switch_dies"
    }
  }
}
channelswitch.fieldInformation = {
  on_value = {
    fieldType="integer"
  },
  off_value = {
    fieldType="integer"
  }
}

function channelswitch.sprite(room, entity)
    return {
        drawableSprite.fromTexture("objects/coreFlipSwitch/o1", {
            x=entity.x,
            y=entity.y,
            color = aelperLib.channel_color_tint,
        }),
        aelperLib.channel_spriteicon(entity.x, entity.y),
    }
end

return channelswitch