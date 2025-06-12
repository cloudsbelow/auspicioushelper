local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelMathController"
entity.depth = 2000

local multiOptions = {"ReplacePrevious", "BlockIfActive", "AttachedMultiple", "DetatchedMultiple"}
local activOptions = {"Interval", "Change", "IntervalOrChange", "IntervalAndChange", "Auto"}
entity.placements = {
  {
    name = "Channel Math Controller",
    data = {
      compiled_operations = "",
      run_immediately = false,
      custom_polling_rate = "",
      multi_type = "Block",
      activation_cond = "Auto",
      debug = false,
      notifying_override="",
      run_when_awake=false,
    }
  }
}
entity.fieldInformation = {
    compiled_operations = {
        options = {"https://cloudsbelow.neocities.org/celestestuff/mathcompiler"}
    },
    multi_type = {
      options = multiOptions,
      editable = false,
    },
    activation_cond = {
      options = activOptions,
      editable = false,
    }
}

entity.texture = "loenn/auspicioushelper/controllers/math"

return entity
