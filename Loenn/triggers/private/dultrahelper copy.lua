local mods = require("mods")

local trigger = {}

trigger.name = "auspicioushelper/speedNormal"
trigger.triggerText = "speednorm"
trigger.placements = {
    name = "Normalize speed (X)",
    data = {
        speed=400,
        min=350,
        max=450,
        dir=0,
        setYSpeed="",
        
        tryOnlyOnce=false,
        applyOnlyOnce=true
    }
}

return trigger