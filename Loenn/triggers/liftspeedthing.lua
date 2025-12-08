local mods = require("mods")

local trigger = {}

trigger.name = "auspicioushelper/LiftspeedTrigger"
trigger.triggerText = "liftspeed"
trigger.placements = {
    name = "liftspeed configurator",
    data = {
      dontGiveLiftspeed=false,
      disableMultiboost=false,
      gracetimeMultiplier=1,
      speedMultiplier=1,
      coverRoom = false,
    }
}

return trigger