local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local channelblock = {}

channelblock.name = "auspicioushelper/ChannelBlock"
channelblock.depth = 2000

channelblock.placements = {
  {
    name = "main",
    data = {
      width = 8,
      height = 8,
      channel = "",
      inverted = false,
      safe = false,
      --alwayspresent = false
    }
  }
}

function channelblock.sprite(room, entity)
    return {
        drawableRectangle.fromRectangle("bordered", entity.x, entity.y, entity.width, entity.height, 
            entity.inverted and aelperLib.channel_color_dark_halfopacity or aelperLib.channel_color, 
            entity.inverted and aelperLib.channel_color_halfopacity or aelperLib.channel_color_dark), 
        aelperLib.channel_spriteicon_entitycenter(entity),
    }
end

return channelblock