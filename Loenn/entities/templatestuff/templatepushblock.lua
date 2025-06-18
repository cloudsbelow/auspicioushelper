local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplatePushBlock")
entity.depth = -13000

entity.placements = {
  {
    name = "Template Pushblock",
    data = {
      template = "",
      depthoffset=5,

      NoPhysicsTime = 0.3,
      speedFromDash = 100,
      horizontalDrag = 300,
      movementLeniency = 4,
      startDisconnected = true,
      hitSprings = true,
      terminalVelocity = 130,
      gravity = 500,
      BounceStrengthFromWall = 0.4,
      ownSpringRecoil = 0,
      ImpactSfx="event:/game/general/fallblock_impact",
    }
  }
}

function entity.rectangle(room, entity)
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end
entity.draw = aelperLib.get_entity_draw("tblk")

return entity