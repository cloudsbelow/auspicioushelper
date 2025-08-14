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

      NoPhysicsTime = "0.3",
      speedFromDash = 100,
      horizontalDrag = "300",
      movementLeniency = 4,
      startDisconnected = true,
      hitSprings = true,
      alwaysDrag = false,
      terminalVelocity = 130,
      gravity = 500,
      BounceStrengthFromWall = 0.4,
      ownSpringRecoil = 0,
      ImpactSfx="event:/game/general/fallblock_impact",
      hitJumpthrus=true,
      throughDashblocks=false,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        }
    }
end

entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tpush")

return entity