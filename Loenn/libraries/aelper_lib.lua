local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local entities = require("entities")
local triggers = require("triggers")
local decals = require("decals")
local utils = require("utils")
local logging = require("logging")
local depths = require("consts.object_depths")
local celesteRender = require("celeste_render")
local autotiler = require("autotiler")
local atlases = require("atlases")
local loadedState = require("loaded_state")
local colors = require("consts.colors")

--#####--

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
if not $(viewMenu):find(item -> item[1] == "auspicioushelper_showtemplates_global") then
    table.insert(viewMenu,{
        "auspicioushelper_showtemplates_global",
        function() settings.auspicioushelper_showtemplates_global = not settings.auspicioushelper_showtemplates_global end,
        "checkbox",
        function() return settings.auspicioushelper_showtemplates_global or false end
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
if settings.auspicioushelper_showtemplates_global == nil then 
    settings.auspicioushelper_showtemplates_global = true
end

--#####--

local aelperLib = {}

local dark_multiplier = 0.65

function delete_template(entity, oldName)
    --aelperLib.log("deleting:", entity.template_name)
    for k,v in ipairs(templates) do
        aelperLib.log(k, #v)
    end
    
    for k, v in ipairs(templates[oldName or entity.template_name] or {}) do
        if v == entity then
            table.remove(templates[oldName or entity.template_name], k)
            --break
        end
    end
    if #(templates[oldName or entity.template_name] or {nil}) == 0 then
        templates[oldName or entity.template_name] = nil
    end
end

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
    if template_name == nil then return end--room isnt zztemplates
    templates[template_name] = templates[template_name] or {}
    
    for _,v in ipairs(templates[template_name]) do
        if v[1] == entity then return end
    end
    table.insert(templates[template_name], {entity, room})
end
aelperLib.template_entity_names = {}
aelperLib.register_template_name = function(name)
    aelperLib.template_entity_names[name]=true
    return name
end
aelperLib.get_template_options = function(entity)
    local paths = {}
    for k,_ in pairs(templates) do
        local first = true
        local roomName
        for name in string.gmatch(k, "([^/]+)") do
            if first then
                paths[name] = paths[name] or {}
                roomName=name
                first=false
            else
                paths[roomName][name] = true
                break
            end
        end
    end

     local toReturn = {}
--     if entity.template == "" then
--         for k,_ in pairs(paths) do
--             table.insert(toReturn, k)
--         end
--     else
--         for room in string.gmatch(entity.template, "([^/]+)") do
--             for k,_ in pairs(paths[room]) do
--                 table.insert(toReturn, room.."/"..k)
--             end
--             break
--         end
--     end
        for k,_ in pairs(paths) do
            for k2,_ in pairs(paths[k]) do
                table.insert(toReturn, k.."/"..k2)
            end
        end

    return toReturn
end
aelperLib.template_selection = function(room, entity)
    local nodes = {}
    for _,node in ipairs(entity.nodes or {}) do
        table.insert(nodes, utils.rectangle(node.x-6, node.y-6, 12, 12))
    end
    
    return utils.rectangle(entity.x-6, entity.y-6, 12, 12), nodes
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
        data[1].x - ((data[1].nodes or {{x=data[1].x}})[1] or {x=data[1].x}).x,
        data[1].y - ((data[1].nodes or {{y=data[1].y}})[1] or {y=data[1].y}).y,
    }
    for _,entity in ipairs(data[2].entities) do
        pcall(function() 
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
                local toInsert = ({entities.getEntityDrawable(movedEntity._name, entities.registeredEntities[movedEntity._name], room, movedEntity, 
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
                        local visibility = entities.nodeVisibility("entities", movedEntity)
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
        end)
    end
    for _,entity in ipairs(data[2].triggers) do
        pcall(function() 
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
                table.insert(toDraw, {
                    func=triggers.getDrawable(nil, triggers.registeredTriggers[entity._name], data[2], movedEntity, {__auspicioushelper_alreadyDrawn=alreadyDrawn})[1],
                    depth=(type(entity.depth) == "func" and entity.depth(room, movedEntity, nil) or entity.depth) or 0})
                    --todo
            end
        end)
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
    for tx = 1, data[1].width/8 do
        for ty = 1, data[1].height/8 do
            if (tx+data[1].x/8<1 or ty+data[1].y/8<1 or tx+data[1].x/8>data[2].width/8 or ty+data[1].y/8>data[2].height/8) == false then
                local tile = data[2].tilesFg.matrix:getInbounds(tx+math.floor(data[1].x/8), ty+math.floor(data[1].y/8))
                if tile ~= "0" then
                    local quads, sprites
                    pcall(function()
                        quads, sprites = autotiler.getQuads(tx+math.floor(data[1].x/8), math.floor(ty+data[1].y/8), data[2].tilesFg.matrix,
                            celesteRender.tilesMetaFg, "0", " ", "*", {{0,0}}, "", autotiler.checkTile)
                        -- "0" is air tile, " " is emptyTile, "*" is wildcard, {{0,0}} is defaultQuad, "" is defaultSprite, 
                    end)
                    
                    if quads == nil then
                        table.insert(toDraw, {
                            func=drawableRectangle.fromRectangle("bordered", 
                                math.floor(tx-1+offset[1]/8)*8+x+0.5, math.floor(ty-1+offset[2]/8)*8+y+0.5, 
                                7,7,
                                {0.8,0.8,0.8},{1,1,1}),
                            depth=depths.fgTerrain})
                    else
                        local quadCount = #quads
    
                        if quadCount > 0 then
                            local randQuad = quads[utils.mod1(celesteRender.getRoomRandomMatrix(data[2], "tilesFg")
                                :getInbounds(tx+math.floor(data[1].x/8), ty+math.floor(data[1].y/8)), quadCount)]
                            local texture = celesteRender.tilesMetaFg[tile].path or " "
                            
                            table.insert(toDraw, {
                                func={
                                    draw=function()
                                        love.graphics.draw(atlases.gameplay[texture].image, 
                                            celesteRender.getOrCacheTileSpriteQuad(celesteRender.tilesSpriteMetaCache, 
                                                tile, texture, randQuad, true),  --true is if this tileset is fg tiles
                                                math.floor(tx-1+offset[1]/8)*8+x+0.5, math.floor(ty-1+offset[2]/8)*8+y+0.5)
                                    end
                                },
                                depth=depths.fgTerrain
                            })
                        end
                    end
                end
                local tile = data[2].tilesBg.matrix:getInbounds(tx+math.floor(data[1].x/8), ty+math.floor(data[1].y/8))
                if tile ~= "0" then
                    local quads, sprites
                    pcall(function()
                        local quads, sprites = autotiler.getQuads(tx+math.floor(data[1].x/8), ty+math.floor(data[1].y/8), data[2].tilesBg.matrix,
                            celesteRender.tilesMetaBg, "0", " ", "*", {{0,0}}, "", autotiler.checkTile)
                        -- "0" is air tile, " " is emptyTile, "*" is wildcard, {{0,0}} is defaultQuad, "" is defaultSprite, 
                    end)
                    
                    if quads == nil then
                        table.insert(toDraw, {
                            func=drawableRectangle.fromRectangle("bordered", 
                                math.floor(tx-1+offset[1]/8)*8+x+0.5, math.floor(ty-1+offset[2]/8)*8+y+0.5, 
                                7,7,
                                {0.8,0.8,0.8},{1,1,1}),
                            depth=depths.fgTerrain})
                    else
                        local quadCount = #quads
    
                        if quadCount > 0 then
                            local randQuad = quads[utils.mod1(celesteRender.getRoomRandomMatrix(data[2], "tilesBg")
                                :getInbounds(tx+math.floor(data[1].x/8), ty+math.floor(data[1].y/8)), quadCount)]
                            local texture = celesteRender.tilesMetaBg[tile].path or " "
                            
                            table.insert(toDraw, {
                                func={
                                    draw=function()
                                        love.graphics.draw(atlases.gameplay[texture].image, 
                                            celesteRender.getOrCacheTileSpriteQuad(celesteRender.tilesSpriteMetaCache, 
                                                tile, texture, randQuad, false),  --true is if this tileset is fg tiles
                                                math.floor(tx-1+offset[1]/8)*8+x+0.5, math.floor(ty-1+offset[2]/8)*8+y+0.5)
                                    end
                                },
                                depth=depths.bgTerrain
                            })
                        end
                    end
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
    if string.sub(room.name, 1, #"zztemplates-") ~= "zztemplates-" then return nil end
    return string.sub(room.name, #"zztemplates-"+1).."/"..entity.template_name
end
aelperLib.get_entity_draw = function(icon_name)
    return function(room, entity, viewport)
        if entity._loenn_display_template == nil then entity._loenn_display_template = true end
        
        local shouldError = false
        if settings.auspicioushelper_showtemplates_global and
            "zztemplates-"..string.sub(entity.template,1,#room.name-#"zztemplates-") == room.name then
            for _,maybeFiller in pairs(room.entities) do
                if maybeFiller._name == "auspicioushelper/templateFiller" and
                    entity.x>=maybeFiller.x and entity.y>=maybeFiller.y and
                    entity.x<maybeFiller.x+maybeFiller.width and entity.y<maybeFiller.y+maybeFiller.height and
                    entity.template == string.sub(room.name,#"zztemplates-"+1).."/"..maybeFiller.template_name then
                        
                    shouldError=true
                end
            end
        end
        if not shouldError and entity._loenn_display_template and settings.auspicioushelper_showtemplates_global then
            shouldError = aelperLib.draw_template_sprites(entity.template, entity.x, entity.y, room, 
            false, viewport and viewport.__auspicioushelper_alreadyDrawn).recursiveError --todo: replace false with whether or not this entity is slected
        end
            
        if icon_name ~= nil or shouldError then
            drawableSprite.fromTexture(shouldError and "loenn/auspicioushelper/template/error" or aelperLib.getIcon("loenn/auspicioushelper/template/"..icon_name), {
                x=entity.x,
                y=entity.y,
            }):draw()
        end
    end
end
local hasLegacy = {
    tblk=true,
    tcass=true,
    tchan=true,
    tfake=true,
    tfall=true,
    tmoon=true,
    tmovr=true,
    tstat=true,
    tswap=true,
    tzip=true
}
aelperLib.getIcon = function(name)
    return (settings.auspicioushelper_legacyicons and hasLegacy[name]) and (name.."_legacy") or name
end

aelperLib.log = function(...)
    local toPrint = "[Auspicious Helper] "
    for i,v in ipairs({...}) do
        if i ~= 1 then
            toPrint = toPrint..", "
        end
        toPrint = toPrint..tostring(v)
    end

    logging.info(toPrint)
end

--#####--

local initialTemplatesLoad = false
local orig_celesteRender_drawMap = celesteRender.drawMap
function celesteRender.drawMap(state)
    if not initialTemplatesLoad then
        initialTemplatesLoad = true
        orig_celesteRender_drawMap(state)
    end
    return orig_celesteRender_drawMap(state)
end

local origLoadFile = loadedState.loadFile
function loadedState.loadFile(fileName, roomName)
    initialTemplatesLoad = false
    for k,_ in pairs(templates) do templates[k]=nil end
    return origLoadFile(fileName, roomName)
end

return aelperLib
