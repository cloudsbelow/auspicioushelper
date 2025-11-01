local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local channelmover = {}

channelmover.name = "auspicioushelper/ChannelMover"
channelmover.depth = 2000
channelmover.nodeLimits = {1,1}
channelmover.nodeLineRenderType = "none"
channelmover.nodeVisibility = "always"

channelmover.placements = {
  {
    name = "Channel Mover",
    data = {
      width = 8,
      height = 8,
      channel = "",
      move_time=1.8,
      asymmetry=1.0,
      safe=false
    }
  }
}

function channelmover.sprite(room, entity)
    return {
        drawableRectangle.fromRectangle("bordered", entity.x,entity.y,entity.width,entity.height,
            aelperLib.channel_color, aelperLib.channel_color_dark),
        aelperLib.channel_spriteicon_entitycenter(entity),
    }
end
function channelmover.nodeSprite(room, entity, node)
    return {
        drawableRectangle.fromRectangle("bordered", node.x,node.y,entity.width,entity.height,
            aelperLib.channel_color_halfopacity, aelperLib.channel_color_dark_halfopacity),
        aelperLib.channel_spriteicon(node.x+entity.width/2, node.y+entity.height/2),
        drawableLine.fromPoints({entity.x+entity.width/2, entity.y+entity.height/2, node.x+entity.width/2, node.y+entity.height/2}, 
            aelperLib.channel_color_dark_halfopacity, 1),
    }
end
function channelmover.selection(room, entity)
    local nodes = {}
    for _,node in ipairs(entity.nodes) do
        table.insert(nodes, utils.rectangle(node.x, node.y, entity.width, entity.height))
    end
    
    return utils.rectangle(entity.x, entity.y, entity.width, entity.height), nodes
end

return channelmover