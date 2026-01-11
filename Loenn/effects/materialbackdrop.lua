local effect = {}

effect.name = "auspicioushelper/MaterialEffect"
effect.canBackground = true
effect.canForeground = true

effect.defaultData = {
    identifier="",
    passes="null",
    params="",
    textures="",
    renderOrder=100000000,
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
        fieldType = "list"
    },
    textures = {
        fieldType = "list"
    },
    params = {
        fieldType = "list"
    }
}

return effect