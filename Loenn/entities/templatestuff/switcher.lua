

local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = "auspicioushelper/TemplateFillerSwitcher"
entity.depth = 2000

local chooseModes = {"Loop", "PseudoRandom", "TrueRandom", "Channel"}
local loopResets = {"Individual", "Room", "Never"}
entity.placements = {
  {
    name = "Template Filler Switcher",
    data = {
      template_name = "",
      channel = "",
      SelectionMode = "Loop",
      LoopResetMode = "Room",
      templates = ""
    }
  }
}

entity.fieldInformation = function(entity)
    return {
        SelectionMode = {options = chooseModes, editable=false},
        LoopResetMode = {options = loopResets, editable=false},
        templates = {
            fieldType="list",
            elementDefault="",
            elementOptions = {
                fieldType = "string",
                options=aelperLib.get_template_options(entity)
            }
        }
    }
end

entity.texture = "loenn/auspicioushelper/controllers/evilrooms"

return entity