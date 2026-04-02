local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local channelhotcoldwatcher = {}

channelhotcoldwatcher.name = "auspicioushelper/ChannelHotColdWatcher"
channelhotcoldwatcher.depth = 2000

channelhotcoldwatcher.placements = {
  {
    name = "Channel Coremode Watcher",
    data = {
      channel = "",
      Hot_value = 0,
      Cold_value = 1
    }
  }
}

channelhotcoldwatcher.texture="loenn/auspicioushelper/hotcoldwatcher"

return channelhotcoldwatcher