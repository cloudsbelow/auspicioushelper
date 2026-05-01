local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateMoveblock")
entity.depth = -13000
entity.nodeLimits = {0,-1}
entity.nodeLineRenderType = "fan"
entity.nodeVisibility = "always"

local directions = {"down","up","left","right"}

entity.placements = {
  {
    name = "main",
    data = {
      template = "",
      depthoffset=0,
      direction="right",
      uncollidable_blocks=false,
      speed="75",
      acceleration="300",
      respawning=true,
      respawn_timer="2",
      Max_stuck="0.15",
      cansteer=false,
      movesfx = "event:/game/04_cliffside/arrowblock_move",
      arrow_texture = "small",
      decal_depth = -10001,
      decal_colors = "#50cf50ff, #ffff",
      max_leniency=4,
      hitJumpthrus=true,
      throughDashblocks=false,
      triggerFromRiding=true,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldOrder = {
  "x","y", "template","depthoffset","speed","acceleration","direction","arrow_texture","decal_depth","decal_colors","max_leniency","Max_stuck"
}
entity.fieldInformation = function(entity)
    return {
        direction = {
            options = directions,
            editable=false
          },
          movesfx = { options = {"event:/game/04_cliffside/arrowblock_move"} },
          respawn_timer = {minimumValue=0},
          max_leniency = {fieldType="integer"},
          
          arrow_texture = {
              options = {
                  "small",
                  "big",
                  "huge",
              }
          },
        template = {
            options = aelperLib.get_template_options(entity)
        }
    }
end

entity.selection = aelperLib.template_selection

entity.draw = aelperLib.get_entity_draw("tmovr")
function entity.nodeTexture(room, entity)
    return "objects/auspicioushelper/templates/movearrows/"
        .. entity.arrow_texture
        .. ({
            ["left"] = "04",
            ["right"] = "00",
            ["up"] = "02",
            ["down"] = "06"
        })[entity.direction]
end
entity.nodeDepth = -100000

function entity.rotate(room, entity, direction)
  entity.direction = ({
    ["left"]="top",
    ["top"]="right",
    ["right"]="bottom",
    ["bottom"]="left",
  })[entity.direction] or "right"
  if direction ~= 0 then
      entity.rotation = ((entity.rotation or 0) + direction * 90) % 360
  end
  return direction ~= 0
end

return entity