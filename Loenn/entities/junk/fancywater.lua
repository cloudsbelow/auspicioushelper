local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local xnaColors = require("consts.xna_colors")

local entity = {}

entity.name = "auspicioushelper/water"
entity.depth = -9999

entity.placements = {
  {
    name = "fancy water",
    data = {
      width = 8,
      height=8,
      templateDrag=1,
      triggerRiding = true,
      jumpOutSides = true,
      hasBottom = true,
      hasTop = true,
      hasLeft = true,
      hasRight = true,
      colors = "6CA4C8CC, 283D4B4C, 517B9699",
      rayLength = "8,64",
      rayWidth = "4,12",
      rayDirection = "60",
      backwardsRays = false,
      verticalShrink = true,
      die = false,
    }
  }
}

entity.fillColor = {xnaColors.LightBlue[1] * 0.3, xnaColors.LightBlue[2] * 0.3, xnaColors.LightBlue[3] * 0.3, 0.5}
entity.borderColor = {xnaColors.LightBlue[1]*0.8,xnaColors.LightBlue[2] * 0.8, xnaColors.LightBlue[3] * 0.8, 0.8}

entity.fieldOrder = {
  "x", "y", "width", "height", "templateDrag", "triggerRiding", "jumpOutSides",
  "hasTop", "hasLeft", "hasBottom", "hasRight", "colors", "rayDirection",
  "rayLength", "rayWidth"
}
entity.fieldInformation = {
  colors={
    fieldType="list",
    elementDefault="6acc"
  }
}

local entity2 = {}
entity2.name = "auspicioushelper/waterCopy"
entity2.depth = -9998

entity2.placements = {
  {
    name = "fancy water (copy settings)",
    data = {
      width = 8,
      height=8,
      hasBottom = true,
      hasTop = true,
      hasLeft = true,
      hasRight = true,
      verticalShrink = true,
    }
  }
}

entity2.fillColor = {xnaColors.Blue[1] * 0.3, xnaColors.Blue[2] * 0.3, xnaColors.Blue[3] * 0.3, 0.5}
entity2.borderColor = {xnaColors.Blue[1]*0.8,xnaColors.Blue[2] * 0.8, xnaColors.Blue[3] * 0.8, 0.8}

return {entity, entity2};