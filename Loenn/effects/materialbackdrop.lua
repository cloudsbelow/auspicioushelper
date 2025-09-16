local effect = {}

effect.name = "auspicioushelper/MaterialEffect"
effect.canBackground = true
effect.canForeground = true

effect.defaultData = {
    identifier="",
    passes="",
    params="",
    textures="",
    renderOrder=100000000,
    quadFirst=true,
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
    }
}

return effect