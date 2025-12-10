local mods = require("mods")

local trigger = {}

trigger.name = "auspicioushelper/ConditionalStrawbCollectTrigger"
trigger.triggerText = function(room, self) return "force collect cond berry \""..self.strawberry_id.."\"" end
trigger.placements = {
    name = "main",
    data = {
        strawberry_id="",
        playerOnSafeGround=false,
    }
}

return trigger