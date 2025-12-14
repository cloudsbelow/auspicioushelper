

import re

TOKEN_RE = re.compile(
    r'''
    \s*(?:
        (?P<brace>[{}])
        |(?P<equals>=)
        |(?P<comma>,)
        |(?P<string>"(?:\\.|[^"])*")
        |(?P<number>-?\d+(?:\.\d+)?)
        |(?P<name>[A-Za-z_][A-Za-z0-9_]*)
    )
    ''',
    re.VERBOSE,
)

def tokenize(text):
    for m in TOKEN_RE.finditer(text):
        kind = m.lastgroup
        yield kind, m.group(kind)

class Parser:
    def __init__(self, tokens):
        self.tokens = list(tokens)
        self.i = 0

    def peek(self):
        return self.tokens[self.i] if self.i < len(self.tokens) else (None, None)

    def pop(self):
        tok = self.peek()
        self.i += 1
        return tok

    def parse_value(self):
        kind, val = self.peek()

        if kind == "brace" and val == "{":
            return self.parse_table()
        elif kind in ("string", "number", "name"):
            self.pop()
            return val
        else:
            raise SyntaxError(f"Unexpected token {kind}:{val}")

    def parse_table(self):
        self.pop()  # {
        items = []
        mapping = {}

        while True:
            kind, val = self.peek()
            if kind == "brace" and val == "}":
                self.pop()
                break

            # key = value ?
            next_kind, _ = self.tokens[self.i + 1]
            if kind == "name" and next_kind == "equals":
                key = val
                self.pop()  # key
                self.pop()  # =
                value = self.parse_value()
                mapping[key] = value
            else:
                items.append(self.parse_value())

            kind, _ = self.peek()
            if kind == "comma":
                self.pop()

        return mapping if mapping else items

def merge_single(old, new, keep_keys, mergeExtra=True, remKeys = []):
    # Dict (keyed table)
    assert(isinstance(new, dict) and isinstance(old,dict))
    result = {}
    for k, v in new.items():
        if k in keep_keys and k in old:
            result[k] = old[k]
        else:
            result[k] = v
    if mergeExtra:
      for k in keep_keys:
          if k not in result and k in old:
              result[k] = old[k]
    for k in remKeys:
        result.pop(k)
            
    return result

def format_value(v, indent=0):
    pad = "  " * indent

    if isinstance(v, dict):
        lines = ["{"]
        for k, val in v.items():
            lines.append(f"{',' if len(lines)>1 else ''}\n{pad}  {k} = {format_value(val, indent + 1)}")
        lines.append('\n'+pad + "}")
        return "".join(lines)

    if isinstance(v, list):
        lines = ["{\n"+pad+"  "]
        for item in v:
            lines.append(f"{', ' if len(lines)>1 else ''}{format_value(item, indent+1)}")
        lines.append(f"\n{pad}}}")
        return "".join(lines)

    return str(v)


res = Parser(tokenize("""
{
    {
        _editorLayer = 0,
        _fromLayer = "entities",
        _id = 2046,
        _name = "auspicioushelper/spinner",
        _type = "entity",
        color = "Red",
        customColor = "ffffff",
        depth = -8500,
        dreamThru = true,
        fancy = "(#a80000,#ff4f4f:0.75,#ff9eb0)-(826,b497d1:0.8,d2c2e2)",
        makeFiller = true,
        neverClip = true,
        numDebris = 4,
        x = 325,
        y = 296
    }
}
""")).parse_value()[0]

keepKeys = ["x","y","width","height","makeFiller","dreamThru","depth"]
remKeys = []

with open("Tools/text/source.txt", "r") as f:
    result = [merge_single(x,res,keepKeys,remKeys=remKeys) for x in Parser(tokenize(f.read())).parse_value()]
    with open("Tools/text/out.txt","w") as w:
        w.write(format_value(result))