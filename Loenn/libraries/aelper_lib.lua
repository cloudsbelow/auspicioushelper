local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local entities = require("entities")
local decals = require("decals")
local utils = require("utils")
local logging = require("logging")
local depths = require("consts.object_depths")
local matrix = require("utils.matrix")
local state = require("loaded_state")

local templates = {}

local settings = require("mods").getModSettings("auspicioushelper")
local menubar = require("ui.menubar").menubar
local viewMenu = $(menubar):find(menu -> menu[1] == "view")[2]
local editMenu = $(menubar):find(menu -> menu[1] == "edit")[2]
if not $(viewMenu):find(item -> item[1] == "auspicioushelper_legacyicons") then
    table.insert(viewMenu,{
        "auspicioushelper_legacyicons",
        function() settings.auspicioushelper_legacyicons = not settings.auspicioushelper_legacyicons end,
        "checkbox",
        function() return settings.auspicioushelper_legacyicons or false end
    })
end
if false and not $(editMenu):find(item -> item[1] == "auspicioushelper_cleartemplatecache") then
    table.insert(editMenu,{
        "auspicioushelper_cleartemplatecache",
        function() 
            for k, _ in pairs(templates) do templates[k] = nil end
            templates={}
        end,
        "checkbox",
        function() return false end
    })
end

--#####--

local dark_multiplier = 0.65

function delete_template(entity, oldName)
    for k, v in ipairs(templates[oldName or entity.template_name] or {}) do
        if v == entity then
            table.remove(templates[oldName or entity.template_name], k)
            break
        end
    end
    if #(templates[oldName or entity.template_name] or {nil}) == 0 then
        templates[oldName or entity.template_name] = nil
    end
end


local aelperLib = {}
aelperLib.channel_color = {230/255, 167/255, 50/255}
aelperLib.channel_color_halfopacity = {aelperLib.channel_color[1], aelperLib.channel_color[2], aelperLib.channel_color[3], 0.5}
aelperLib.channel_color_dark = {aelperLib.channel_color[1]*dark_multiplier, aelperLib.channel_color[2]*dark_multiplier, aelperLib.channel_color[3]*dark_multiplier}
aelperLib.channel_color_dark_halfopacity = {aelperLib.channel_color_dark[1], aelperLib.channel_color_dark[2], aelperLib.channel_color_dark[3], 0.5}
aelperLib.channel_color_tint = {1-(1-aelperLib.channel_color[1])*0.5, 1-(1-aelperLib.channel_color[2])*0.5, 1-(1-aelperLib.channel_color[3])*0.5, 1}
aelperLib.channel_spriteicon = function(x,y)
    return drawableSprite.fromTexture("loenn/auspicioushelper/channel_icon", {
        x=x, y=y
    })
end
aelperLib.channel_spriteicon_entitycenter = function(entity)
    return aelperLib.channel_spriteicon(entity.x+(entity.width or 0)/2, entity.y+(entity.height or 0)/2)
end
aelperLib.update_template = function(entity, room, data)
    data = data or {}
    if data.deleting then 
        delete_template(entity)
        return
    end

    if data.oldName then delete_template(entity, oldName) end
    local template_name = aelperLib.templateID_from_entity(entity, room)
    templates[template_name] = templates[template_name] or {}
    
    table.insert(templates[template_name], {entity, room})
end
local template_entity_names = {}
aelperLib.register_template_name = function(name)
    template_entity_names[name]=true
    return name
end
aelperLib.draw_template_sprites = function(name, x, y, room, selected, alreadyDrawn)
    alreadyDrawn = alreadyDrawn or {}
    
    local data = (templates[name] or {})[1]
    if data == nil then return {} end
    if alreadyDrawn[data[1]._id] then 
        alreadyDrawn.recursiveError=true
        return alreadyDrawn
    end
    
    local toDraw = {}
    local offset = {
        data[1].x - (data[1].nodes or {{x=data[1].x}})[1].x,
        data[1].y - (data[1].nodes or {{y=data[1].y}})[1].y,
    }
    for _,entity in ipairs(data[2].entities) do
        if not alreadyDrawn[entity._id] and 
            entity.x > data[1].x-(entity.width or 0.01) and entity.x < data[1].x+data[1].width and
            entity.y > data[1].y-(entity.height or 0.01) and entity.y < data[1].y+data[1].height then
                
            alreadyDrawn[entity._id]=true
    
            local movedEntity = utils.deepcopy(entity)
            movedEntity.x=x + (entity.x - data[1].x) + offset[1]
            movedEntity.y=y + (entity.y - data[1].y) + offset[2]
            if movedEntity.nodes then
                for _,node in ipairs(movedEntity.nodes) do
                    node.x = x + (node.x - data[1].x) + offset[1]
                    node.y = y + (node.y - data[1].y) + offset[2]
                end
            end
            local toInsert = ({entities.getEntityDrawable(movedEntity._name, nil, room, movedEntity, 
                {__auspicioushelper_alreadyDrawn=alreadyDrawn})})[1]
            if toInsert.draw == nil then 
                for _,v in ipairs(toInsert) do table.insert(toDraw, {
                    func=v,
                    depth=(type(entity.depth) == "func" and entity.depth(room, movedEntity, nil) or entity.depth) or 0}) end
            else table.insert(toDraw, {
                func=toInsert,
                depth=(type(entity.depth) == "func" and entity.depth(room, movedEntity, nil) or entity.depth) or 0})
            end
        
            if movedEntity.nodes then
                for index,node in ipairs(movedEntity.nodes) do
                    local visibility = entities.nodeVisibility(nil, movedEntity)
                    if visibility == "always" or (visibility == "selected" and selected) then 
                    
                        toInsert = ({entities.getNodeDrawable(movedEntity._name, nil, room, movedEntity, node, index, nil)})[1]
                        if toInsert.draw == nil then 
                            for _,v in ipairs(toInsert) do table.insert(toDraw, {
                                func=v,
                                depth=(type(entity.depth) == "func" and entity.depth(room, movedEntity, nil) or entity.depth) or 0}) end
                        else table.insert(toDraw, {
                            func=toInsert,
                            depth=(type(entity.depth) == "func" and entity.depth(room, movedEntity, nil) or entity.depth) or 0})
                        end
                    end
                end
            end
        end
    end
    for _,entity in ipairs(data[2].decalsBg) do
        if entity.x >= data[1].x-(entity.width or 0) and entity.x <= data[1].x+data[1].width and
            entity.y >= data[1].y-(entity.height or 0) and entity.y <= data[1].y+data[1].height then
    
            local movedEntity = utils.deepcopy(entity)
            movedEntity.x=x + (entity.x - data[1].x) + offset[1]
            movedEntity.y=y + (entity.y - data[1].y) + offset[2]
            local toInsert = ({decals.getDrawable(entity.texture, nil, room, movedEntity, nil)})[1]
            table.insert(toDraw, {func=toInsert, depth=entity.depth or depths.bgDecals})
        end
    end 
    for _,entity in ipairs(data[2].decalsFg) do
        if entity.x >= data[1].x-(entity.width or 0) and entity.x <= data[1].x+data[1].width and
            entity.y >= data[1].y-(entity.height or 0) and entity.y <= data[1].y+data[1].height then
    
            local movedEntity = utils.deepcopy(entity)
            movedEntity.x=x + (entity.x - data[1].x) + offset[1]
            movedEntity.y=y + (entity.y - data[1].y) + offset[2]
            local toInsert = ({decals.getDrawable(entity.texture, nil, room, movedEntity, nil)})[1]
            table.insert(toDraw, {func=toInsert, depth=entity.depth or depths.fgDecals})
        end
    end 
    for tx = -1, data[1].width/8+2 do
        for ty = -1, data[1].height/8+2 do
            if (tx<=0 or ty<=0 or tx>data[1].width/8 or ty>data[1].height/8) == false then
                if data[2].tilesFg.matrix:getInbounds(tx+data[1].x/8, ty+data[1].y/8) ~= "0" then
                    table.insert(toDraw, {
                        func=drawableRectangle.fromRectangle("bordered", (tx-1)*8+x+offset[1]+0.5,(ty-1)*8+y+offset[2]+0.5, 7,7,
                            {0.8,0.8,0.8},{1,1,1}),
                        depth=depths.fgTerrain})
                end
                if data[2].tilesBg.matrix:getInbounds(tx+data[1].x/8, ty+data[1].y/8) ~= "0" then
                    table.insert(toDraw, {
                        func=drawableRectangle.fromRectangle("bordered", (tx-1)*8+x+offset[1]+0.5,(ty-1)*8+y+offset[2]+0.5, 7,7,
                            {0.5,0.5,0.5},{0.6,0.6,0.6}),
                        depth=depths.bgTerrain})
                end
            end
        end
    end
    
    table.sort(toDraw, function (a, b)
        return a.depth > b.depth
    end)
    
    for _,v in ipairs(toDraw) do
        v.func:draw() 
    end

    return alreadyDrawn
end
aelperLib.templateID_from_entity = function(entity, room)
    return string.sub(room.name, #"zztemplates-"+1).."/"..entity.template_name
end
aelperLib.get_entity_draw = function(icon_name)
    return function(room, entity, viewport)
        if entity._loenn_display_template == nil then entity._loenn_display_template = true end
        
        local shouldError = false
        if "zztemplates-"..string.sub(entity.template,1,#room.name-#"zztemplates-") == room.name then
            for _,maybeFiller in pairs(room.entities) do
                if maybeFiller._name == "auspicioushelper/templateFiller" and
                    entity.x>=maybeFiller.x and entity.y>=maybeFiller.y and
                    entity.x<maybeFiller.x+maybeFiller.width and entity.y<maybeFiller.y+maybeFiller.height and
                    entity.template == string.sub(room.name,#"zztemplates-"+1).."/"..maybeFiller.template_name then
                        
                    shouldError=true
                end
            end
        end
        if not shouldError and entity._loenn_display_template then shouldError = aelperLib.draw_template_sprites(entity.template, entity.x, entity.y, room, 
            false, viewport and viewport.__auspicioushelper_alreadyDrawn).recursiveError end--todo: replace false with whether or not this entity is slected
            
        drawableSprite.fromTexture(shouldError and "loenn/auspicioushelper/template/error" or aelperLib.getIcon("loenn/auspicioushelper/template/"..icon_name), {
            x=entity.x,
            y=entity.y,
        }):draw()
    end
end

aelperLib.getIcon = function(name)
    return settings.auspicioushelper_legacyicons and (name.."_legacy") or name
end

return aelperLib
