local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelSprite"
entity.depth = 2000
entity.nodeLimits = {0,1}
entity.nodeLineRenderType = "line"

entity.placements = {
  {
    name = "Channel Image",
    data = {
      depth=2,
      origin = "0,0",
      scale = "1,1",
      rotation="0",
      image_path = "decals/3-resort/chair_c",
      tint = "fff",
      materialIdentifiers="",
    }
  },
  {
    name = "Channel Sprite",
    data = {
      depth=2,
      origin = "0,0",
      scale = "1,1",
      rotation="0",
      channel = "",
      animationNames = "case0, case1, case2",
      xml_spritename = "auspicioushelper_example1",
      tint = "fff",
      materialIdentifiers="",
    }
  }
}
entity.fieldInformation = function (entity)
  local ret = {
    materialIdentifiers = {fieldType="list",elementDefault = ""},
    depth = {fieldType="integer"}
  }
  if entity.xml_spritename == nil then else
    ret.animationNames = {fieldType="list",elementDefault = ""}
  end
  return ret
end
entity.fieldOrder = function (entity)
  if entity.xml_spritename == nil then
    return {"x","y","depth","origin","scale","rotation","image_path","tint","materialIdentifiers"} 
  else 
    return {"x","y","depth","origin","scale","rotation","channel","animationNames","xml_spritename","tint","materialIdentifiers"}
  end
end

entity.texture = function(room,entity)
  if entity.xml_spritename == nil then
    return entity.image_path
  else
    return "loenn/auspicioushelper/controllers/sprite"
  end
end
return entity