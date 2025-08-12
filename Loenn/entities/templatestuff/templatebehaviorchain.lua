local drawableSprite = require("structs.drawable_sprite")
local drawableText = require("structs.drawable_text")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateBehaviorChain")
entity.depth = -13000
entity.nodeLimits = {0,-1}
entity.nodeLineRenderType = "line"
entity.nodeVisibility = "always"


entity.placements = {
  {
    name = "Template Behavior Chain",
    data = {
      template = "",
      
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
    

function entity.selection(room, entity)
    local nodeRects = {}    
    for _, node in ipairs(entity.nodes) do
        table.insert(nodeRects, utils.rectangle(node.x-4,node.y-4,8,8))
    end
    return utils.rectangle(entity.x-8,entity.y-8,16,16), nodeRects
end
function entity.nodeSprite(room, entity, node)
    if entity._loenn_preset_initializerType ~= nil then
        initPreset(entity)
    end
    
    local existsValidTemplate = false
    local reason = "Template not found"
    for _,entity in ipairs(room.entities) do
        if aelperLib.template_entity_names[entity._name] and
            entity.x == node.x and entity.y == node.y then
            reason = "\"template\" field not empty"
            if entity.template == "" then
                existsValidTemplate=true
                break
            end
        end
    end
    if existsValidTemplate then return drawableSprite.fromTexture("loenn/auspicioushelper/template/tgroupnode", 
        {x=node.x, y=node.y, depth=-13001}) end
    return {
        drawableText.fromText(reason, node.x-20, node.y-30, 40, 18, nil, nil, "ff4444"),
        drawableSprite.fromTexture("loenn/auspicioushelper/template/group_error", {x=node.x, y=node.y, depth=-13001}),
        drawableSprite.fromTexture("loenn/auspicioushelper/template/tgroupnode", {x=node.x, y=node.y, color="ff4444", depth=-13001})
    }
end
entity.draw = aelperLib.get_entity_draw("tgroup")

function initPreset(entity)
    entity._loenn_preset_initializerType = false
    
    entity.nodes = {
        {x=16,y=16},
        {x=32,y=32},
        {x=48,y=48},
    }
end

return entity