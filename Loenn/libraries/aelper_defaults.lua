
local defaults = {
  defaultShaders = {
    "null", "ausp/maskBy", "ausp/maskedFrom", "ausp/blurH", "ausp/blurV", "ausp/invertColor", "ausp/invertAlpha", "ausp/innerBorder", "ausp/outerBorder", "ausp/flip", "ausp/static", "ausp/tint", "ausp/rainbowify", "ausp/opacity", "ausp/colorgrade", "ausp/colorgradefade", "ausp/compose"
  }, 
  easings = {
    "Linear","SineIn","SineOut","SineInOut","QuadIn","QuadOut","CubeIn","CubeOut","Smoothstep","QuartIn","QuartOut","QuintIn","QuintOut"
  },
  splineTypes = {
    "simpleLinear","compoundLinear","centripetalNormalized","centripetalDenormalized","uniformNormalized","uniformDenormalized"
  },

  soundIndices = {
    {"None", 0},
    {"Asphalt", 1},
    {"Car", 2},
    {"Dirt", 3},
    {"Snow", 4},
    {"Wood", 5},
    {"Bridge", 6},
    {"Girder", 7},
    {"Brick", 8},
    {"Traffic Block", 9},

    {"Dreamblock Inactive", 11},
    {"Dreamblock Active", 12},

    {"Resort Wood", 13},
    {"Resort Roof", 14},
    {"Resort Platforms", 15},
    {"Resort Basement", 16},
    {"Resort Laundry", 17},
    {"Resort Boxes", 18},
    {"Resort Books", 19},
    {"Resort Forcefield", 20},
    {"Resort Clutterswitch", 21},
    {"Resort Elevator", 22},

    {"Cliffside Snow", 23},
    {"Cliffside Grass", 25},
    {"Cliffside Whiteblock", 27},

    {"Glass", 32},
    {"Grass", 33},

    {"Cassette Block", 35},

    {"Core Ice", 36},
    {"Core Rock", 37},

    {"Glitch", 40},
    {"Internet Cafe", 42},
    {"Cloud", 43},
    {"Moon", 44},
  }
}

return defaults