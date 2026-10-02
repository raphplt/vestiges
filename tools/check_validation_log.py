#!/usr/bin/env python3
"""Contrôle commun : journal non vide, aucune erreur inattendue, une fin explicite."""
from pathlib import Path
import re
import sys


def check(path, pattern, expected=1):
    text = re.sub(r"\x1b\[[0-9;]*m", "", Path(path).read_text(errors="replace"))
    if not text.strip():
        raise ValueError("journal vide")
    errors = []
    for line in text.splitlines():
        # Erreur Godot connue uniquement à la fermeture forcée ; aucune exception n'est masquée.
        if re.match(r"^ERROR: \d+ resources still in use at exit", line):
            continue
        if re.search(r"^\s*(?:ERROR|SCRIPT ERROR):|Unhandled exception|System\.[\w.]+Exception"
                     r"|^\[[^]]+\] FAIL(?:\s|$)|RESULT.*(?:failures=[1-9]\d*|valid=False)", line):
            errors.append(line)
    if errors:
        raise ValueError("erreurs inattendues :\n" + "\n".join(errors[:12]))
    count = len(re.findall(pattern, text, re.MULTILINE))
    if count != expected:
        raise ValueError(f"{count} marqueur(s) de fin, {expected} attendu(s) : {pattern}")


if __name__ == "__main__":
    try:
        check(sys.argv[1], sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 1)
    except (OSError, ValueError) as error:
        print(f"Validation échouée ({sys.argv[1]}) : {error}", file=sys.stderr)
        sys.exit(1)
