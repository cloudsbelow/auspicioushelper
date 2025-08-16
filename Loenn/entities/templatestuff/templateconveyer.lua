local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}
local splineTypes = {"simpleLinear","compoundLinear","centripetalNormalized","centripetalDenormalized","uniformNormalized","uniformDenormalized"}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateBelt")
entity.depth = -13000
entity.nodeLimits = {1,-1}
entity.nodeLineRenderType = "line"

entity.placements = {
  {
    name = "Template Belt",
    data = {
      template = "",
      depthoffset=5,
      speed=0.3,
      numPerSegment=3,
      initialOffset=0,
      loop=false,
      spline = "uniformNormalized",
      lastNodeIsKnot = true,
      channel = "",
      
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
entity.draw = aelperLib.get_entity_draw("tconv")

return entity