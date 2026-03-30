local defaults = require("mods").requireFromPlugin("libraries.aelper_defaults")

local effect = {}

effect.name = "auspicioushelper/MaterialEffect"
effect.canBackground = true
effect.canForeground = true

local blendmodes = {"Auto","Discard","AlphaBlend","Addative","Multiply","Darken","Lighten","Subtract","Max","Min"}
local fades = {"Always","Never","Linear","Sqrt","Cos"}

effect.defaultData = {
    identifier="",
    passes="null",
    params="",
    textures="",
    renderOrder="",
    quadFirst=false,
    alwaysRender=true,
    blendMode="AlphaBlend",
    fadeIn="Always",
    fadeOut="Always",
    extraFadeOutTime=0,
    clearColor="#0000"
}

effect.fieldOrder = {
    "only", "exclude", "flag", "notflag",
    "passes", "params", "textures", "identifier",
    "renderOrder","blendMode", "quadFirst", "alwaysRender","tag",
    "fadeIn","fadeOut","extraFadeOutTime","clearColor"
}

effect.fieldInformation = {
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
    },
    blendMode = {
        options = blendmodes,
        editable=false
    },
    fadeIn = {
        options = fades,
        editable=false,
    },
    fadeOut = {
        options = fades,
        editable=false,
    },
}

return effect