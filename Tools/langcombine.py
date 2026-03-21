

import os
import json

ROOT_DIR = "./Tools/lang"         # directory containing subdirectories with json files
OUTPUT_LANG = "./Tools/text/enparseugh.lang"


# ----------------------------
# HELPERS
# ----------------------------

def format_header(title):
    return [
        "#"*(10+len(title)),
        f"###  {title}  ###",
        "#"*(10+len(title)),
        ""
    ]


def format_entity_header(entity_name):
    return [f"# {entity_name}"]


def emit_basic(entity_key, entity_obj, lines):
    name = entity_obj.get("name", "")
    desc = entity_obj.get("description", "")

    if name:
        lines.append(f"{entity_key}.placements.name.main={name}")
    if desc:
        lines.append(f"{entity_key}.placements.description.main={desc}")


def emit_attributes(entity_key, placements, lines):
    for attr, value in placements.items():
        name_key = f"{entity_key}.attributes.name.{attr}"
        desc_key = f"{entity_key}.attributes.description.{attr}"

        if isinstance(value, list):
            if len(value)>0:
                lines.append(f"{name_key}={value[0]}")
            if len(value)>1:
                lines.append(f"{desc_key}={value[1]}")

        else:
            if value:
                lines.append(f"{desc_key}={value}")

    if placements:
        lines.append("")


# ----------------------------
# MAIN
# ----------------------------

def generate_lang():
    lines = []

    for root, dirs, files in os.walk(ROOT_DIR):
        for file in files:
            if not file.endswith(".json"):
                continue

            path = os.path.join(root, file)

            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)

            # --- section header ---
            title = os.path.splitext(file)[0]
            lines.extend(format_header(title))

            # --- entities ---
            for entity_key, entity_obj in data.items():
                entity_name = entity_key.split(".", 1)[1]

                lines.extend(format_entity_header(entity_name))

                emit_basic(entity_key, entity_obj, lines)
                emit_attributes(entity_key, entity_obj.get("placements", {}), lines)

            lines.append("")  # spacing between files

    # --- write output ---
    with open(OUTPUT_LANG, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print(f"Lang file written to {OUTPUT_LANG}")


if __name__ == "__main__":
    generate_lang()