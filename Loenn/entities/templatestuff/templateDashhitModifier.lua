local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateDashhitModifier")
entity.depth = -13000

local rtypes = {"Normal","Bounce","Rebound","Bumper","NormalTrigger","BounceTrigger","ReboundTrigger","BumperTrigger"}

entity.placements = {
  {
    name = "Template Dashhit Modifier",
    data = {
      template = "",
      depthoffset=0,
      skipChannel="",
      Left="Normal",
      Right="Normal",
      Up="Normal",
      Down="Normal",
      refillOptions="0",
      alwaysPropegate=true,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        },

      Left = {options=rtypes, editable=false},
      Right = {options=rtypes, editable=false},
      Up = {options=rtypes, editable=false},
      Down = {options=rtypes, editable=false},
    }
end

entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tdash")

return entity