"""
Accès uniforme aux modèles du pipeline : personnages (squelette humanoïde) et créatures (gabarits libres).
Un modèle fournit son cadre, son pivot au sol, ses matériaux, ses animations et une fonction état → volumes.
"""
from __future__ import annotations

import importlib
from dataclasses import dataclass
from typing import Any, Callable, Sequence

from PIL import Image

from .palette import Material
from .render import FRAME_PIVOT, FRAME_SIZE, Part, render
from .rig import build_skeleton


@dataclass(frozen=True)
class SpriteModel:
    model_id: str
    frame_size: tuple[int, int]
    pivot: tuple[float, float]
    materials: Sequence[Material]
    animations: dict[str, list[Any]]
    parts: Callable[[Any], list[Part]]

    def render(self, state: Any, yaw: float) -> Image.Image:
        return render(self.parts(state), self.materials, yaw, self.frame_size, self.pivot)


def character(character_id: str) -> SpriteModel:
    module = importlib.import_module(f"tools.sprites.characters.{character_id}")
    return SpriteModel(character_id, FRAME_SIZE, FRAME_PIVOT, module.MATERIALS, module.ANIMATIONS,
                       lambda pose: module.build(build_skeleton(pose, module.DIMENSIONS)))


def creature(enemy_id: str) -> SpriteModel:
    module = importlib.import_module(f"tools.sprites.creatures.{enemy_id}")
    return SpriteModel(enemy_id, module.FRAME_SIZE, module.FRAME_PIVOT, module.MATERIALS, module.ANIMATIONS,
                       module.parts)
