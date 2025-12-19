



local mods = require("mods")

local trigger = {}

trigger.name = "auspicioushelper/DreamTransitionEnabler"
trigger.triggerText = "Dream Trans"
trigger.placements = {
    name = "Dream Transition Enabler",
    data = {
        assistSmuggle=false,
        prioTemplateDreamblocks=false,
    }
}

return trigger