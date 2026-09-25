"""
Accès uniforme aux modèles du pipeline : personnages (squelette humanoïde) et créatures (gabarits libres).
Un modèle fournit son cadre, son pivot au sol, son échelle, ses matériaux, ses animations et une fonction état → volumes.
"""
from __future__ import annotations

import importlib
from dataclasses import dataclass
from typing import Any, Callable, Sequence

from PIL import Image

from .palette import Material
from .render import (CHARACTER_FRAME_PIVOT, CHARACTER_FRAME_SIZE, CHARACTER_MODEL_SCALE, MODEL_SCALE, Part, render,
                     render_layers)
from .rig import build_skeleton


@dataclass(frozen=True)
class SpriteModel:
    model_id: str
    frame_size: tuple[int, int]
    pivot: tuple[float, float]
    scale: float
    materials: Sequence[Material]
    animations: dict[str, list[Any]]
    parts: Callable[[Any], list[Part]]

    def render(self, state: Any, yaw: float) -> Image.Image:
        return render(self.parts(state), self.materials, yaw, self.frame_size, self.pivot, self.scale)

    def render_layers(self, state: Any, yaw: float) -> list[Image.Image]:
        return render_layers(self.parts(state), self.materials, yaw, self.frame_size, self.pivot, self.scale)


def character(character_id: str) -> SpriteModel:
    module = importlib.import_module(f"tools.sprites.characters.{character_id}")
    return SpriteModel(character_id, CHARACTER_FRAME_SIZE, CHARACTER_FRAME_PIVOT, CHARACTER_MODEL_SCALE,
                       module.MATERIALS, module.ANIMATIONS,
                       lambda pose: module.build(build_skeleton(pose, module.DIMENSIONS)))


def creature(enemy_id: str) -> SpriteModel:
    module = importlib.import_module(f"tools.sprites.creatures.{enemy_id}")
    return SpriteModel(enemy_id, module.FRAME_SIZE, module.FRAME_PIVOT, MODEL_SCALE, module.MATERIALS,
                       module.ANIMATIONS, module.parts)
