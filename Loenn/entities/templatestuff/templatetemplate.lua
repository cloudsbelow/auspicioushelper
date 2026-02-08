local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateTemplate")
entity.depth = -13000

entity.fieldInformation = function(entity)
    return {
    replacements={
        fieldType="list",
        elementDefault="KEY",
    },
        template = {
            options = aelperLib.get_template_options(entity)
        }
    }
end

entity.placements = {
  {
    name = "Template Template",
    data = {
      template = "",
      depthoffset=0,
      replacements="",
      
      _loenn_display_template = true,
    }
  }
}
entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tsub")

return entity