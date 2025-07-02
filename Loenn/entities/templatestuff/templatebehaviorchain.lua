local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateBehaviorChain")
entity.depth = -12999
entity.nodeLimits = {0,-1}
entity.nodeLineRenderType = "line"
entity.nodeVisibility = "always"


entity.placements = {
  {
    name = "Template Behavior Chain",
    data = {
      template = "",
      
      _loenn_display_template = true,
    }
  }
}
function entity.rectangle(room, entity)
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end
function entity.nodeTexture(room, entity, node)
  return "loenn/auspicioushelper/template/tgroupnode"
end
entity.draw = aelperLib.get_entity_draw("tgroup")
function entity.nodeTexture(room, entity)
    return "loenn/auspicioushelper/template/tgroupnode"
end

return entity