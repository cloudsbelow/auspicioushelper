local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local channelBooster = {}
channelBooster.name = "auspicioushelper/ChannelBooster"
channelBooster.depth = 2000

channelBooster.placements = {
  {
    name = "main",
    data = {
      state0 = "normal",
      state1 = "normal",
      triggerTemplates = false,
      give_liftspeed = false,
      channel = "",
      self_activating = false,
      state0_customsprite = "",
      state1_customsprite = "",
    }
  }
}
local boosterTypes = {"normal","reversed","red","redEvict","none"}
channelBooster.fieldInformation = {
  state0 ={
    options = boosterTypes,
    editable=false
  },
  state1 = {
    options = boosterTypes,
    editable=false
  }
}

function channelBooster.sprite(room, entity)
    return {
        drawableSprite.fromTexture(({
            normal = "objects/auspicioushelper/channelbooster/blackwhole00",
            reversed = "objects/auspicioushelper/channelbooster/whitewhole00",
            none = "objects/auspicioushelper/channelbooster/booster/outline",
            red = "objects/booster/boosterRed00",
            redEvict = "objects/booster/boosterRed00",
        })[entity.state1],{
            x=entity.x+3,
            y=entity.y+3,
            color = {1,1,1,0.5}
        }), 
        drawableSprite.fromTexture(({
            normal = "objects/auspicioushelper/channelbooster/blackwhole00",
            reversed = "objects/auspicioushelper/channelbooster/whitewhole00",
            none = "objects/auspicioushelper/channelbooster/booster/outline",
            red = "objects/booster/boosterRed00",
            redEvict = "objects/booster/boosterRed00",
        })[entity.state0],{
            x=entity.x,
            y=entity.y,
        }), 
        aelperLib.channel_spriteicon_entitycenter(entity),
    }
end
function channelBooster.rectangle(room, entity)
    return utils.rectangle(entity.x-9,entity.y-9, 18, 18)
end

return channelBooster
