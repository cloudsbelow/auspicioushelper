local enums = require("consts.celeste_enums")

local trigger = {}

trigger.name = "auspicioushelper/FadeChannelTrigger"
trigger.fieldInformation = {
    positionMode = {
        options = enums.trigger_position_modes,
        editable = false
    }
}
trigger.placements = {
    name = "channel fade",
    data = {
        from="0",
        to="1",
        positionMode = "NoEffect",
        onlyOnce = false,
        channel="fade",
        activeChannel=""
    }
}
trigger.fieldOrder = {"x","y","width","height","channel","positionMode","from","to"}

return trigger