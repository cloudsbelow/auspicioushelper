local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelMathController"
entity.depth = 2000
entity.nodeLimits = {0,-1}
entity.nodeLineRenderType = "line"

local multiOptions = {"ReplacePrevious", "BlockIfActive", "AttachedMultiple", "DetatchedMultiple"}
local activOptions = {"Interval", "Change", "IntervalOrChange", "IntervalAndChange", "Auto","OnlyAwake"}
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
      only_run_for_nonzero=false,
    }
  }
}
entity.fieldInformation = {
    compiled_operations = {
        options = {"https://cloudsbelow.neocities.org/celestestuff/visualmathcompiler"}
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
