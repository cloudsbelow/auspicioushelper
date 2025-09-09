local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local channelclearcontroller = {}

channelclearcontroller.name = "auspicioushelper/ChannelClearController"
channelclearcontroller.depth = 2000

channelclearcontroller.placements = {
  {
    name = "Channel Clear Controller",
    data = {
      channel = "",
      value = 0,
      clear_all = false,
      clear_prefix="",
      advanced=""
    }
  }
}
channelclearcontroller.fieldInformation = {
  channel = {
    fieldType="string"
  },
  value = {
    fieldType="integer"
  }
}
channelclearcontroller.texture = "loenn/auspicioushelper/controllers/clear"

return channelclearcontroller