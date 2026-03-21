import os
import re
import json

LANG_FILE = "./Loenn/lang/en_gb.lang"        # existing lang file (optional)


# ----------------------------
# LANG PARSER
# ----------------------------

def parse_lang_file(path):
    lang = {}

    if not os.path.exists(path):
        return lang

    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or "=" not in line:
                continue

            key, value = line.split("=", 1)
            lang[key.strip()] = value.strip()

    return lang


# ----------------------------
# LUA PARSER (simple + strict)
# ----------------------------

def parse_lua_file(path):
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    entity_name = None
    placement_name = None
    in_data = False
    brace_depth = 0
    attributes = []

    for line in lines:
        stripped = line.strip()

        
        # --- extract name ---
        if ".name" in stripped and "=" in stripped:
            match = re.search(r'=\s*"([^"]+)"', stripped)
            if match:
                entity_name = match.group(1)
            match = re.search(r'=\s*aelperLib.register_template_name\("([^"]+)"\)', stripped)
            if match:
                entity_name = match.group(1)
        else:
            match =  re.search(r'\s*name\s*=\s*"([^"]+)"', stripped)
            if match:
                placement_name = match.group(1)
        

        # --- detect start of data block ---
        if "data = {" in stripped:
            in_data = True
            brace_depth = 1
            continue

        if in_data:
            brace_depth += stripped.count("{")
            brace_depth -= stripped.count("}")

            # extract keys like: key = value,
            if "=" in stripped:
                key = stripped.split("=")[0].strip()
                if key and key not in attributes:
                    attributes.append(key)

            if brace_depth == 0:
                in_data = False

    if not entity_name:
        print(f"[ERROR] No entity name found in {path}")
        return None

    if not attributes:
        print(f"[WARNING] No attributes found in {path}")

    return entity_name, placement_name, attributes


# ----------------------------
# LANG LOOKUP HELPERS
# ----------------------------

def get_lang_entry(lang, base_key):
    name_key = f"{base_key}.attributes.name"
    desc_key = f"{base_key}.attributes.description"

    # NOTE: we append .<attr> later
    return name_key, desc_key


def resolve_attribute(lang, entity_key, attr):
    name_key = f"{entity_key}.attributes.name.{attr}"
    desc_key = f"{entity_key}.attributes.description.{attr}"

    has_name = name_key in lang
    has_desc = desc_key in lang

    if has_name and has_desc:
        return [lang[name_key], lang[desc_key]]
    elif has_name:
        return [lang[name_key]]
    elif has_desc:
        return lang[desc_key]
    else:
        return ""


def resolve_basic(lang, key):
    return lang.get(key, "")


# ----------------------------
# MAIN GENERATION
# ----------------------------
exclude = ["width","height"]
force = {
    "_loenn_display_template":["[LÖNN] Display Template"],
    "template":"The name of the template to use. (<room>/<name>). If left empty, can be used in behavior chains and in connected tiles.",
    "depthoffset":"How much to offset the depth of contained entities. Accumulates from parent to child.",
    "lastNodeIsKnot":"Make the last node a knot even if it is not stacked. Can be disabled to allow for crooked paths on the last segment of a looping path. Having it on results in more intuitive behavior for new users.",
    "spline":"The spline type to use. Linear types move in straight lines. The catmull variants curve smoothly between nodes with sharp edges at knots (created by stacking two nodes on the same pixel)",
    "throughDashblocks":"Whether to move through breakable blocks like dashblocks or breakable template blocks",
    "hitJumpthrus":"Whether jumpthrus should block this entity's movement. Works for sideways and upsidedown jumpthrus (and regular)."
}

def noneOrEmpty(string):
    return string is None or string==""

def generate(out_path, lua_dir, isTrigger=False):
    lang = parse_lang_file(LANG_FILE)

    result = {}
    if os.path.exists(out_path):
        with open(out_path, "r", encoding="utf-8") as f:
            try:
                result = json.load(f)
            except json.JSONDecodeError:
                print("[WARNING] Existing JSON invalid, starting fresh")
                result = {}

    for filename in os.listdir(lua_dir):
        if not filename.endswith(".lua"):
            continue

        path = os.path.join(lua_dir, filename)

        parsed = parse_lua_file(path)
        if not parsed:
            continue

        entity_name, placement_name, attributes = parsed

        entity_key = f"entities.{entity_name}" if not isTrigger else f"triggers.{entity_name}"

        entity_obj = result.get(entity_key, {})

        # --- name / description ---
        langName = resolve_basic(lang, f"{entity_key}.placements.name.main")
        if placement_name!="main":
            if(langName is not None and langName != ""):
                print(f"{entity_key} has a lang file name despite placement name not being main")
            if not noneOrEmpty(entity_obj.get("name")):
                print(f"{entity_key} has a key in the existing json despite placement name not being main")
            elif entity_obj.get("name") == "":
                print(f"pop unneeded name from {entity_key}")
                entity_obj.pop("name")
        elif not entity_obj.get("name"):
            entity_obj["name"] = langName

        if not entity_obj.get("description"):
            entity_obj["description"] = resolve_basic(lang, f"{entity_key}.placements.description.main")

        # --- attributes ---
        placements = entity_obj.get("placements", {})

        for attr in attributes:
            # only add if missing
            if attr not in placements:
                placements[attr] = resolve_attribute(lang, entity_key, attr)

        for ex in exclude:
            if ex in placements:
                placements.pop(ex)
        for k,v in force.items():
            if k in placements:
                placements[k]=v

        entity_obj["placements"] = placements

        result[entity_key] = entity_obj

    # --- write output ---
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=4, ensure_ascii=False)

    print(f"Done. Wrote {out_path}")


if __name__ == "__main__":
    toParse = [
        "junk", "misc", "channelstuff", "templatestuff"
    ]
    for str in toParse:
        generate("./Tools/lang/"+str+".json","./Loenn/entities/"+str)
    generate("./Tools/lang/triggers.json","./Loenn/triggers", True)