local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local channelplayerwatcher = {}

channelplayerwatcher.name = "auspicioushelper/ChannelPlayerWatcher"
channelplayerwatcher.depth = 2000

local ops = {"xor", "and", "or", "set", "max", "min", "add"}
local actions = {"dash"}
local modes = {"custom", "dashAttacking", "grounded", "ducking", "state", "dead", "speed", "holding"}

channelplayerwatcher.placements = {
  {
    name = "Channel Player Watcher",
    data = {
      channel = "",
      valueWhenMissing = 0,
      mode = "dashAttacking",
      custom = ""
    }
  }
}
channelplayerwatcher.fieldInformation = {
  mode = {
    options=modes,
    editable=false,
  },
  valueWhenMissing = {
    fieldType="integer"
  }
}
channelplayerwatcher.texture = "loenn/auspicioushelper/controllers/playerwatcher"

return channelplayerwatcher