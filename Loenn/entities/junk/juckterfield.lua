
local entity = {}

entity.name = "auspicioushelper/JuckterField"
entity.depth = -12000
entity.nodeLimits = {1,-1}
entity.nodeLineRenderType = "fan"
entity.nodeVisibility = "always"

entity.placements = {
  {
    name = "Juckter Field",
    data = {
      width = 8,
      height=8
    }
  }
}

entity.fillColor = {0.4,0.4,0.4, 0.6}
entity.borderColor = {1,1,1, 0.8}

return entity