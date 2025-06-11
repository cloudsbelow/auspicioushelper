local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/templateholdable")
entity.depth = -100000
entity.nodeLimits = {0,1}
entity.nodeLineRenderType = "none"

entity.placements = {
  {
    name = "Template Holdable",
    data = {
      width = 8,
      height = 8,
      template = "",
      depthoffset=5,
      cannot_hold_timer=0.1,
      Holdable_collider_expand=4,
      slowfall=false,
      slowrun=true,
      always_collidable=false,
      player_momentum_weight=1.0,
      holdable_momentum_weight=0.0,
      wallhitsound="event:/game/05_mirror_temple/crystaltheo_hit_side",
      wallhit_speedretain=0.4,
      gravity=800,
      terminal_velocity=200,
      friction =350,
      die_to_barrier = false,
      respawning = false,
      dontFlingOff = false,
      respawnDelay = 1.5,
      start_floating = false,
      dangerous = false,
      voidDieOffset = 100,
      tutorial = false,
      
      _loenn_display_template = true,
    }
  }
}

function entity.selection(room, entity)
    local node = {}
    if entity.nodes[1] then
        node = {utils.rectangle(entity.x+entity.nodes[1].x-3, entity.y+entity.nodes[1].y-3, 6,6)}
    end
    return utils.rectangle(entity.x, entity.y, entity.width, entity.height), node
end
function entity.rectangle(room, entity)
  return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
end
function entity.draw(room, entity, viewport)
    local offset = entity.nodes and entity.nodes[1] or {x=entity.width/2, y=entity.height/2}
    aelperLib.draw_template_sprites(entity.template, entity.x+offset.x, entity.y+offset.y, room)
--     drawableSprite.fromTexture(aelperLib.getIcon("loenn/auspicioushelper/template/tmoon"), {
--         x=entity.x,
--         y=entity.y,
--     }):draw()
    drawableRectangle.fromRectangle("bordered", entity.x+0.5, entity.y+0.5, entity.width-1, entity.height-1,
        {0.4,0.9,0.4,0.3}, {0.5,1,0.5,1}):draw()
end

function entity.nodeRectangle(room,entity,node,nodeIndex)
  return utils.rectangle(entity.x+node.x-3,entity.y+node.y-3,6,6)
end
function entity.nodeAdded(room, entity, nodeIndex)
    table.insert(entity.nodes, {x=entity.width/2, y=entity.height/2})
    return true
end
function entity.nodeSprite(room, entity, node)
    return {
        drawableRectangle.fromRectangle("bordered", entity.x+node.x-3, entity.y+node.y-3, 6, 6, 
            {0.4,0.9,0.4,0.3}, {0.5,1,0.5,1}), 
        drawableLine.fromPoints({
            entity.x, entity.y,
            entity.x+node.x, entity.y+node.y
        }, {0.5,1,0.5,1}, 1)
    }
end

return entity