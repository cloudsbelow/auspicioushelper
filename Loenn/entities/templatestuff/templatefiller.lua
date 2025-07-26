local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = "auspicioushelper/templateFiller"
entity.depth = -100000
entity.nodeLimits = {0,1}
entity.nodeLineRenderType = "none"

entity.placements = {
  {
    name = "main",
    data = {
      width = 8,
      height = 8,
      template_name = "",
      
      oldTemplateName = nil,
    }
  }
}
entity.fieldInformation = {
    oldTemplateName = {
        fieldType = "string",
        valueTransformer = function() return nil end,
        displayTransformer = function() return "" end,
        editable = false,
    }
}
entity.ignoredFields = {"oldTemplateName"}

function entity.sprite(room, entity)
    if entity._loenn_display_template ~= nil then entity._loenn_display_template = nil end
  
    if entity.oldTemplateName ~= entity.template_name then
        aelperLib.update_template(entity, room, {oldName=entity.oldTemplateName})
    end
    entity.oldTemplateName = aelperLib.templateID_from_entity(entity, room)
    
    return drawableRectangle.fromRectangle("bordered", entity.x, entity.y, entity.width, entity.height, 
        {195/255, 138/255, 255/255,0.3}, {195/255*0.65, 138/255*0.65, 255/255*0.65,1}) 
end

function entity.nodeRectangle(room,entity,node,nodeIndex)
    return utils.rectangle(node.x-3,node.y-3,6,6)
end

function entity.nodeSprite(room, entity, node)
    return {
        drawableRectangle.fromRectangle("bordered", node.x-3, node.y-3, 6, 6, 
            {195/255, 138/255, 255/255,0.3}, {195/255*0.65, 138/255*0.65, 255/255*0.65,1}), 
        drawableLine.fromPoints({
            entity.x, entity.y,
            node.x, node.y
        }, {195/255*0.65, 138/255*0.65, 255/255*0.65,1}, 1)
    }
end
function entity.nodeAdded(room, entity, nodeIndex)
    table.insert(entity.nodes, {x=entity.x, y=entity.y})
    return true
end

--#####--

function entity.onDelete(room, entity, nodeIndex)
    if nodeIndex ~= 0 then return end
    
    aelperLib.update_template(entity, room, {deleting=true})
end

return entity
