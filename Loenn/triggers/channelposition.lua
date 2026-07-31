local enums = require("consts.celeste_enums")

local trigger = {}

trigger.name = "auspicioushelper/ChannelPosition"
trigger.fieldInformation = {
    positionMode = {
        options = enums.trigger_position_modes,
        editable = false
    }
}
trigger.placements = {
    name = "channel position",
    data = {
        xChannel="x",
        yChannel="y",
        activeChannel = "",
        onlyInside = false,
        path="player",
        Depth=-10
    }
}
trigger.fieldOrder = {"x","y","width","height","xChannel","yChannel","path","activeChannel"}

return trigger