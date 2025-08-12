local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateChannelmover")
entity.depth = -13000
entity.nodeLimits = {1,-1}
entity.nodeLineRenderType = "line"


local easings = {"Linear","SineIn","SineOut","SineInOut","QuadIn","QuadOut","CubeIn","CubeOut","Smoothstep","QuartIn","QuartOut","QuintIn","QuintOut"}
entity.placements = {
  {
    name = "main",
    data = {
      template = "",
      depthoffset=5,
      channel = "",
      move_time=1.8,
      asymmetry=1.0,
      easing = "Linear",
      complete=false,
      alternateEasing=true,
      shake=false,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = function(entity)
    return {
    move_time = {minimumValue=0},
    asymmetry = {minimumValue=0},
    easing = {options=easings, editable=false},
        template = {
            options = aelperLib.get_template_options(entity)
        }
    }
end

function entity.selection(room, entity)
    local nodes = {}
    for _,node in ipairs(entity.nodes) do
        table.insert(nodes, utils.rectangle(node.x-8, node.y-8, 16, 16))
    end
    
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16), nodes
end
entity.draw = aelperLib.get_entity_draw("tchan")

return entity