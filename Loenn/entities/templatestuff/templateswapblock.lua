local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}
local splineTypes = {"simpleLinear","compoundLinear","centripetalNormalized","centripetalDenormalized","uniformNormalized","uniformDenormalized"}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateSwapblock")
entity.depth = -13000
entity.nodeLimits = {1,-1}
entity.nodeLineRenderType = "line"

entity.placements = {
  {
    name = "main",
    data = {
      template = "",
      depthoffset=5,
      max_speed = 360,
      max_return_speed = 144,
      returning = false,
      spline = "simpleLinear",
      lastNodeIsKnot = true,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        },
        spline = {options = splineTypes, editable=true}
    }
end

entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tswap")

return entity