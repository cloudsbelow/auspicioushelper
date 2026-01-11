local defaults = require("aelper_defaults")

local effect = {}

effect.name = "auspicioushelper/MaterialEffect"
effect.canBackground = true
effect.canForeground = true

effect.defaultData = {
    identifier="",
    passes="null",
    params="",
    textures="",
    renderOrder=-1000000,
    quadFirst=false,
    alwaysRender=true,
    reload=false
}

effect.fieldOrder = {
    "only", "exclude", "flag", "notflag",
    "passes", "params", "textures", "identifier",
    "renderOrder", "quadFirst", "alwaysRender", "reload"
}

effect.fieldInformation = {
    renderOrder = {
        fieldType = "integer"
    },
    passes = {
        fieldType = "list",
        elementDefault="null",
        elementOptions={
            fieldType = "string",
            options=defaults.defaultShaders
        }
    },
    textures = {
        fieldType = "list",
        elementDefault = "1:gp"
    },
    params = {
        fieldType = "list",
        elementDefault = "color:#fff"
    }
}

return effect