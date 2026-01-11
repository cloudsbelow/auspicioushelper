local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local defaults = require("aelper_defaults")

local materialcontroller = {}

materialcontroller.name = "auspicioushelper/MaterialController"
materialcontroller.depth = 2000

local ftypes = {"Always","Never","Linear","Cosine","Sqrt"}
materialcontroller.placements = {
  {
    name = "main",
    data = {
      passes="",
      identifier="",
      params="",
      textures="",
      depth = 0,
      Fade_in = "Linear",
      fadeOut = "Linear",
      quadFirst = false,
      always = true,
      drawInScene = true,
      reload=false
    }
  }
}
materialcontroller.fieldInformation = {
  Fade_in = {
    options = ftypes,
    editable = false
  },
  fadeOut = {
    options = ftypes,
    editable = false
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
materialcontroller.texture = "loenn/auspicioushelper/controllers/material"
materialcontroller.fieldOrder = {
  "x","y","passes", "identifier", "params", "textures", "Fade_in", "depth", "fadeOut", "quadFirst", "drawInScene", "always", "reload"
}
return materialcontroller