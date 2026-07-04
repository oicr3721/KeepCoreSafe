from pathlib import Path
from PIL import Image
import json, sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets" / "sprites"
manifest = json.loads((OUT / "manifest.json").read_text(encoding="utf-8"))
palette = {tuple(bytes.fromhex(c[1:])) + (255,) for c in manifest["palette"]}
allowed = palette | {(0, 0, 0, 0)}
results = []

for filename in manifest["sheets"]:
    path = OUT / filename
    im = Image.open(path).convert("RGBA")
    colors = set(im.getdata())
    bad = sorted(colors - allowed)
    opaque_color_count = len({c for c in colors if c[3] == 255})
    frame_boxes = []
    centers = []
    for i in range(4):
        frame = im.crop((i * 64, 0, (i + 1) * 64, 64))
        box = frame.getchannel("A").getbbox()
        frame_boxes.append(box)
        if box:
            centers.append(((box[0] + box[2]) / 2, (box[1] + box[3]) / 2))
    center_spread = [
        round(max(c[j] for c in centers) - min(c[j] for c in centers), 2)
        for j in (0, 1)
    ] if centers else [999, 999]
    # Effects intentionally expand attack/break bounds; base frame placement remains cell-locked.
    alignment_ok = center_spread[0] <= 18 and center_spread[1] <= 18
    item = {
        "file": filename,
        "size": list(im.size),
        "size_ok": im.size == (256, 64),
        "frame_count_ok": len(frame_boxes) == 4 and all(frame_boxes),
        "palette_ok": not bad,
        "opaque_color_count": opaque_color_count,
        "simplified_shading_ok": opaque_color_count <= 4,
        "bad_colors": [list(c) for c in bad],
        "transparent_background_ok": (0, 0, 0, 0) in colors,
        "partial_alpha_absent": all(c[3] in (0, 255) for c in colors),
        "frame_bbox_centers_spread_px": center_spread,
        "alignment_ok": alignment_ok,
        "filtering_absent": not bad and all(c[3] in (0, 255) for c in colors),
    }
    item["passed"] = all(item[k] for k in (
        "size_ok", "frame_count_ok", "palette_ok", "simplified_shading_ok", "transparent_background_ok",
        "partial_alpha_absent", "alignment_ok", "filtering_absent"
    ))
    results.append(item)

report = {
    "required_palette_rgba": [list(c) for c in sorted(palette)],
    "sheet_count": len(results),
    "all_passed": all(x["passed"] for x in results),
    "checks": results,
}
(OUT / "validation_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps({"sheet_count": len(results), "all_passed": report["all_passed"],
                  "failed": [x["file"] for x in results if not x["passed"]]}, indent=2))
sys.exit(0 if report["all_passed"] else 1)
