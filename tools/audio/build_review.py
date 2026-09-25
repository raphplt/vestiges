#!/usr/bin/env python3
"""Reconstruit la page d'écoute autonome à partir du catalogue des candidats."""

import argparse
import html
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--batch", default="lot-a", help="Dossier du lot sous doc/audio (défaut : lot-a)")
    args = parser.parse_args()
    if not re.fullmatch(r"lot-[a-z0-9]+", args.batch):
        raise ValueError("Identifiant de lot invalide")
    review = ROOT / "doc/audio" / args.batch
    data = json.loads((review / "candidates.json").read_text(encoding="utf-8"))
    if data.get("batch", args.batch) != args.batch:
        raise ValueError("Le catalogue ne correspond pas au lot demandé")
    if data.get("schema_version") != 1 or not isinstance(data.get("effects"), list):
        raise ValueError("Catalogue audio absent ou incompatible")
    effect_ids = set()
    for effect in data["effects"]:
        if not isinstance(effect.get("id"), str) or effect["id"] in effect_ids:
            raise ValueError("Identifiant d'effet manquant ou dupliqué")
        effect_ids.add(effect["id"])
        candidate_ids = set()
        for candidate in effect["candidates"]:
            if not isinstance(candidate.get("id"), str) or candidate["id"] in candidate_ids:
                raise ValueError(f"Identifiant de candidat manquant ou dupliqué : {effect['id']}")
            candidate_ids.add(candidate["id"])
            for key in ("preview_path", "original_path"):
                value = candidate.get(key)
                if value and not (review / value).resolve().is_file():
                    raise ValueError(f"Fichier absent : {value}")
    payload = json.dumps(data, ensure_ascii=False).replace("<", "\\u003c")
    template = Path(__file__).with_name("review_template.html").read_text(encoding="utf-8")
    page = template.replace("__BATCH_ID__", args.batch).replace("__BATCH_LABEL__", html.escape(args.batch.replace("lot-", "lot ").upper()))
    page = page.replace("__EFFECT_COUNT__", str(len(effect_ids))).replace("__CATALOG_JSON__", payload)
    (review / "index.html").write_text(page, encoding="utf-8")
    print(f"Page {args.batch} générée : {len(effect_ids)} effets, {sum(len(e['candidates']) for e in data['effects'])} candidats")


if __name__ == "__main__":
    main()
