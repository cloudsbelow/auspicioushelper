
local defaults = require("mods").requireFromPlugin("libraries.aelper_defaults")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/channelTemplateMover"
entity.depth = 2000
entity.nodeLimits = {1,1}
entity.nodeLineRenderType = "line"

entity.placements = {
  {
    name = "channel template gate mover",
    data = {
      activateChannel = "",
      shakeTime = 0,
      arrivalShake = 0,
      moveTime = 1,
      easing = "Linear",
      relative = true,
      log = false,
    }
  }
}
entity.fieldInformation = {
  easing = {options = defaults.easings, editable=false}
}
entity.texture = "loenn/auspicioushelper/controllers/channelflag"

return entity