
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/SillySpikes"
entity.depth = 2000
entity.nodeLimits = {0,1}
entity.nodeLineRenderType = "line"

local rotations = {"up","down","left","right"}
local spikeTypes = {
    "default",
    "outline",
    "cliffside",
    "reflection"
}

entity.placements = {
  {
    name = "relative spikes",
    data = {
      type = "reflection",
      direction = "up",
      fancy = "",
      tint = "#ffffff",
      dreamThru = false,
      dashThru = false,
      useOwnSpeed = true,
      fixPickup = true,
      fixOnBlock = true,
      canAttach=true,
      width = 8,
    }
  }
}
entity.fieldInformation = {
    direction = {
      options = rotations,
      editable = false,
    },
    type = {
      options = spikeTypes,
      editable = true,
    },
    tint = {
      type = "color"
    }
  }

function entity.flip(room, entity, horizontal, vertical)
  if vertical then
    if entity.direction=="up" then entity.direction="down"
    elseif entity.direction=="down" then entity.direction="up" end
  end
  if horizontal then
    if entity.direction=="right" then entity.direction="left" 
    elseif entity.direction=="left" then entity.direction="right" end
  end
  return true
end

function entity.rotate(room, entity, direction)
  if direction ~= 0 then
    if entity.width then
      entity.height = entity.width
      entity.width = nil
    elseif entity.height then
      entity.width = entity.height
      entity.height = nil
    end
    if entity.direction=="left" then entity.direction="up"
    elseif entity.direction=="up" then entity.direction="right"
    elseif entity.direction=="right" then entity.direction="down"
    elseif entity.direction=="down" then entity.direction="left" end
  end
  return direction ~= 0
end

-- Maddies Helping Hand copypaste begin

-- Lönn spike helper copypaste begin
local drawableSprite = require("structs.drawable_sprite")

local spikeTexture = "danger/spikes/%s_%s00"

local spikeOffsets = {
    up = {0, 1},
    down = {0, -1},
    right = {-1, 0},
    left = {1, -1}
}

local spikeJustifications = {
    up = {0.0, 1.0},
    down = {0.0, 0.0},
    right = {0.0, 0.0},
    left = {1.0, 0.0}
}

local function getDirectionJustification(direction)
    local offset = spikeJustifications[direction] or {0, 0}
    return offset[1], offset[2]
end

local function getDirectionOffset(direction)
    local offset = spikeOffsets[direction] or {0, 0}
    return offset[1], offset[2]
end

local function getSpikeSpritesFromTexture(entity, direction, variant, texture)
    local step = 8

    local horizontal = direction == "left" or direction == "right"
    local justificationX, justificationY = getDirectionJustification(direction)
    local offsetX, offsetY = getDirectionOffset(direction)
    local rotation = 0
    local length = horizontal and (entity.height or step) or (entity.width or step)
    local positionOffsetKey = horizontal and "y" or "x"

    local position = {
        x = entity.x,
        y = entity.y
    }

    local sprites = {}

    for i = 0, length - 1, step do
        -- Tentacles overlap instead of "overflowing"
        if i == length - step / 2 then
            position[positionOffsetKey] = position[positionOffsetKey] - step / 2
        end

        local sprite = drawableSprite.fromTexture(texture, position)

        sprite.depth = -1
        sprite.rotation = rotation
        sprite:setJustification(justificationX, justificationY)
        sprite:addPosition(offsetX, offsetY)

        table.insert(sprites, sprite)

        position[positionOffsetKey] = position[positionOffsetKey] + step
    end

    return sprites
end

local function getNormalSpikeSprites(entity, direction)
    local variant = entity.type or "default"
    local texture = string.format(spikeTexture, variant, direction)

    return getSpikeSpritesFromTexture(entity, direction, variant, texture)
end
-- Lönn spike helper copypaste end

function entity.sprite(room, entity)
    return getNormalSpikeSprites(entity, entity.direction)
end

return entity
-- Maddies Helping Hand copypaste end