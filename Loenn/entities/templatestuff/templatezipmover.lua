local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateZipmover")
entity.depth = -13000
entity.nodeLimits = {1,-1}
entity.nodeLineRenderType = "line"

local rtypes = {"loop","none", "normal"}
local atypes = {"ride","rideAutomatic","dash","dashAutomatic","manual"}
local splineTypes = {"simpleLinear","compoundLinear","centripetalNormalized","centripetalDenormalized","uniformNormalized","uniformDenormalized"}

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
      spline = "compoundLinear",
      lastNodeIsKnot = true,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = function(entity)
    return {
        template = {
  return_type ={
    options = rtypes,
    editable=false,
  },
  activation_type={
    options = atypes,
    editable=false,
  },
            options = aelperLib.get_template_options(entity)
        },spline = {options = splineTypes, editable=true}
    }
end

entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tzip")

return entity