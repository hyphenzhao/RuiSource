import json
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

try:
    import mne
except ImportError as exc:
    raise RuntimeError("MNE-Python is required for source preview rendering. Install with: python -m pip install mne matplotlib") from exc


def _normalize_name(value):
    token = "".join(ch for ch in value if ch.isalnum()).upper()
    if token.startswith("EEG"):
        token = token[3:]
    for suffix in ("REF", "LE", "RE"):
        if token.endswith(suffix):
            token = token[: -len(suffix)]
    aliases = {
        "T3": "T7",
        "T4": "T8",
        "T5": "P7",
        "T6": "P8",
    }
    return aliases.get(token, token)


def _match_positions(channel_names):
    montage = mne.channels.make_standard_montage("standard_1020")
    positions = montage.get_positions()["ch_pos"]
    normalized_positions = {_normalize_name(name): (name, np.asarray(position, dtype=float)) for name, position in positions.items()}

    matched = []
    used = set()
    for channel_name in channel_names:
        normalized = _normalize_name(channel_name)
        if normalized in used or normalized not in normalized_positions:
            continue
        canonical_name, position = normalized_positions[normalized]
        used.add(normalized)
        matched.append((canonical_name, position))
    return matched


def _draw_brain(ax):
    u = np.linspace(0, 2 * np.pi, 80)
    v = np.linspace(0, np.pi, 40)
    x = 0.075 * np.outer(np.cos(u), np.sin(v))
    y = 0.100 * np.outer(np.sin(u), np.sin(v))
    z = 0.070 * np.outer(np.ones_like(u), np.cos(v)) + 0.015
    ax.plot_surface(x, y, z, color="#d8e7f7", alpha=0.45, linewidth=0, shade=True)

    head_x = 0.095 * np.outer(np.cos(u), np.sin(v))
    head_y = 0.120 * np.outer(np.sin(u), np.sin(v))
    head_z = 0.095 * np.outer(np.ones_like(u), np.cos(v)) + 0.010
    ax.plot_wireframe(head_x, head_y, head_z, color="#b8b8b8", linewidth=0.35, alpha=0.30, rstride=4, cstride=4)

    ax.plot([0, 0], [0.118, 0.145], [0.035, 0.020], color="#808080", linewidth=1.4)
    ax.plot([-0.014, 0, 0.014], [0.115, 0.145, 0.115], [0.020, 0.040, 0.020], color="#808080", linewidth=1.2)


def _render(channel_names, output_path):
    matched = _match_positions(channel_names)

    fig = plt.figure(figsize=(6.4, 5.2), dpi=150)
    ax = fig.add_subplot(111, projection="3d")
    _draw_brain(ax)

    if matched:
        labels = [item[0] for item in matched]
        xyz = np.vstack([item[1] for item in matched])
        ax.scatter(xyz[:, 0], xyz[:, 1], xyz[:, 2], s=34, c="#1f5ed5", edgecolors="white", linewidths=0.7, depthshade=True)

        for label, position in matched:
            ax.text(position[0], position[1], position[2] + 0.006, label, fontsize=6, color="#202020", ha="center")

    ax.set_title(f"MNE standard EEG source preview ({len(matched)}/{len(channel_names)} channels)", fontsize=10)
    ax.text2D(0.02, 0.02, "Static MNE/matplotlib preview · standard_1020 montage", transform=ax.transAxes, fontsize=7, color="#606060")
    ax.set_xlabel("Left–Right", fontsize=8)
    ax.set_ylabel("Posterior–Anterior", fontsize=8)
    ax.set_zlabel("Inferior–Superior", fontsize=8)
    ax.view_init(elev=22, azim=-62)
    ax.set_box_aspect((1.0, 1.15, 0.9))
    ax.set_xlim(-0.115, 0.115)
    ax.set_ylim(-0.130, 0.155)
    ax.set_zlim(-0.095, 0.130)
    ax.grid(False)
    fig.tight_layout()

    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(output_path, bbox_inches="tight", facecolor="white")
    plt.close(fig)


def main():
    if len(sys.argv) < 2:
        raise RuntimeError("Missing input json path")

    with open(sys.argv[1], "r", encoding="utf-8") as f:
        payload = json.load(f)

    channel_names = payload.get("channel_names", [])
    output_path = payload["output_path"]
    _render(channel_names, output_path)


if __name__ == "__main__":
    main()
