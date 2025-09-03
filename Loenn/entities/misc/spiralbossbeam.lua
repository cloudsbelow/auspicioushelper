local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local spiralbossbeam = {}

spiralbossbeam.name = "auspicioushelper/SpiralBossBeam"
spiralbossbeam.depth = 2000
spiralbossbeam.nodeLineRenderType = "line"

spiralbossbeam.placements = {
  {
    name = "main",
    data = {
      start_angle = 0.,
      speed = 1.,
      depth = -1
    }
  }
}

function spiralbossbeam.sprite(room, entity)
    local toReturn = {}
    
    for i=3,0,-1 do 
        local sprite = drawableSprite.fromTexture("loenn/auspicioushelper/spiralbossbeam", {
            x=entity.x,
            y=entity.y,
            r=entity.start_angle+math.pi/2 - i*entity.speed*0.1,
            color = {1,1,1,1-(i/3)*0.8},
            jy=1,
        })

        table.insert(toReturn, sprite)
    end

    return toReturn
end

function spiralbossbeam.rectangle(room, entity)
    return utils.rectangle(entity.x-8, entity.y-8, 16,16)
end

return spiralbossbeam