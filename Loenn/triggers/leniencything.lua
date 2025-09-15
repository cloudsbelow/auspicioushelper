local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/leniencything"
entity.depth = 2000

entity.placements = {
  {
    name = "Pixel leniency slipping thing",
    data = {
      staticSlip = 1,
      fallingSlip = 1,
      maxGroundedStep = 2,
      maxStepSlope = 1,
      neededFallDist = 2,
      onlyWhenInside=false,
      setOnAwake=false,
    }

  }
}
entity.fieldInformation = {
  staticSlip = {
    fieldType="integer"
  },
  fallingSlip = {
    fieldType="integer"
  },
  maxGroundedStep = {
    fieldType="integer"
  }
}

return entity
