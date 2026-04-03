local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateGluable")
entity.depth = -13000
entity.nodeLimits = {0,-1}
entity.nodeLineRenderType = "line"

local constraintMode = {"None","OnlyX","OnlyY"}
local splineTypes = {"simpleLinear","compoundLinear","centripetalNormalized","centripetalDenormalized","uniformNormalized","uniformDenormalized"}

local function concatTables(a, b)
    local result = {}
    for _, v in ipairs(a) do table.insert(result, v) end
    for _, v in ipairs(b) do table.insert(result, v) end
    return result
end

entity.placements = {
  {
    name = "Template Gluable",
    data = {
      template = "",
      depthoffset=0,
      glue_to_identifier ="",
      maxSpeed = "",
      constraint = "None",
      setProgressChannel="",
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        },
        constraint = {
          options = concatTables(constraintMode,splineTypes),
          editable = false
        }
    }
end

entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tglue")

return entity