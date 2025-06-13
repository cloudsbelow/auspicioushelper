local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateFakewall")
entity.depth = -13000

entity.placements = {
  {
    name = "main",
    data = {
      template = "",
      depthoffset=5,
      freeze = false,
      dontOnTransitionInto = false,
      disappear_depth = -13000,
      fade_speed = 1,
      
      _loenn_display_template = true,
    }
  }
}
function entity.rectangle(room, entity)
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end
entity.draw = aelperLib.get_entity_draw("tfake")

return entity