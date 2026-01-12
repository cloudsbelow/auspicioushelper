
local defaults = {
  defaultShaders = {
    "null", "ausp/maskBy", "ausp/maskedFrom", "ausp/blurH", "ausp/blurV", "ausp/invertColor", "ausp/invertAlpha", "ausp/innerBorder", "ausp/outerBorder", "ausp/flip", "ausp/static", "ausp/tint", "ausp/opacity"
  }, 
  easings = {
    "Linear","SineIn","SineOut","SineInOut","QuadIn","QuadOut","CubeIn","CubeOut","Smoothstep","QuartIn","QuartOut","QuintIn","QuintOut"
  },
  splineTypes = {
    "simpleLinear","compoundLinear","centripetalNormalized","centripetalDenormalized","uniformNormalized","uniformDenormalized"
  }
}

return defaults