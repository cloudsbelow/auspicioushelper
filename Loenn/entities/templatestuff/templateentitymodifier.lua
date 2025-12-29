local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateEntityModifier")
entity.depth = -13000

entity.placements = {
  {
    name = "Template Entity Modifier",
    data = {
      template = "",
      depthoffset=0,
      activeChannel="",
      collidableChannel="",
      visibleChannel="",
      shakeChannel="",
      allowCustomSpeed=false,
      log = false,
      only = ""
    }
  }
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        }
    }
end

entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tentmod")

return entity