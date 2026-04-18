local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateCollisionModifier")
entity.depth = -13000
local combinationModes = {"and", "xor", "or", "typeMinusPath", "pathMinusType"} 

entity.placements = {
  {
    name = "Template Collision Modifier",
    data = {
      template = "",
      depthoffset=0,
      paths="",
      types="*",
      combinationMode="or",
      log=false,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldOrder = {
  "x","y", "template","depthoffset","paths","types"
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        },
        combinationMode = {
          options = combinationModes,
          editable = false
        }
    }
end

entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tcolmod")

return entity