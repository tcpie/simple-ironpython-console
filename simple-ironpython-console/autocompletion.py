import jedi

def get_autocompletion(script, linenum, column):
    analyzed_script = jedi.Script(script, linenum, column, "")
    completions = analyzed_script.completions()

    if len(completions) == 0:
        return ""

    if len(completions) == 1:
        return completions[0].complete

    ret = ""

    for c in completions:
        ret += c.name + "\n"

    return ret
