local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateZipmover")
entity.depth = -13000
entity.nodeLimits = {1,-1}
entity.nodeLineRenderType = "line"

local rtypes = {"loop","none", "normal"}
local atypes = {"ride","rideAutomatic","dash","dashAutomatic"}

entity.placements = {
  {
    name = "main",
    data = {
      template = "",
      depthoffset=5,
      return_type = "normal",
      activation_type = "ride",
      channel = "",
      propegateRiding = false,
      lastNodeIsKnot = true,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = {
  return_type ={
    options = rtypes,
    editable=false,
  },
  activation_type={
    options = atypes,
    editable=false,
  },
}

function entity.selection(room, entity)
    local nodes = {}
    for _,node in ipairs(entity.nodes) do
        table.insert(nodes, utils.rectangle(node.x-8, node.y-8, 16, 16))
    end
    
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16), nodes
end
entity.draw = aelperLib.get_entity_draw("tzip")

return entity