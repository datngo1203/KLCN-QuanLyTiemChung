from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math
import re

ROOT = Path(__file__).resolve().parent
PUML = ROOT / "Hinh_2_9_System_Use_Case_Tong_Quan.puml"
PNG = ROOT / "Hinh_2_9_System_Use_Case_Tong_Quan.png"

W, H = 1800, 1400
BOUNDARY = (300, 120, 1500, 1260)

EXPECTED_ACTORS = {
    "Customer": "Khách hàng",
    "Reception": "Lễ tân",
    "Doctor": "Bác sĩ",
    "Cashier": "Thu ngân",
    "Nurse": "Điều dưỡng",
}

EXPECTED_UCS = {
    "UC09": "Quản lý hồ sơ khách hàng/\\nngười được tiêm",
    "UC10": "Tra cứu dịch vụ\\ntiêm chủng",
    "UC11": "Đăng ký dịch vụ\\ntiêm chủng",
    "UC19": "Theo dõi hành trình\\ntiêm chủng",
    "UC12": "Quản lý lịch hẹn",
    "UC13": "Gửi nhắc lịch",
    "UC15": "Ghi nhận khám sàng lọc\\nvà chỉ định",
    "UC16": "Thanh toán dịch vụ",
    "UC17": "Ghi nhận mũi tiêm",
    "UC18": "Ghi nhận theo dõi\\nsau tiêm",
}

EXPECTED_ASSOCIATIONS = {
    frozenset(("Customer", "UC09")),
    frozenset(("Customer", "UC10")),
    frozenset(("Customer", "UC11")),
    frozenset(("Customer", "UC19")),
    frozenset(("Reception", "UC12")),
    frozenset(("Reception", "UC13")),
    frozenset(("UC15", "Doctor")),
    frozenset(("UC16", "Cashier")),
    frozenset(("UC17", "Nurse")),
    frozenset(("UC18", "Nurse")),
}

ACTOR_POS = {
    "Customer": (145, 440),
    "Reception": (145, 1030),
    "Doctor": (1655, 340),
    "Cashier": (1655, 600),
    "Nurse": (1655, 930),
}

UC_POS = {
    "UC09": (560, 300, 420, 105),
    "UC10": (560, 460, 340, 95),
    "UC11": (560, 620, 350, 95),
    "UC19": (560, 780, 360, 95),
    "UC12": (790, 930, 300, 90),
    "UC13": (790, 1090, 270, 90),
    "UC15": (1180, 330, 430, 105),
    "UC16": (1180, 580, 320, 90),
    "UC17": (1180, 810, 300, 90),
    "UC18": (1180, 1040, 340, 100),
}

FONT_CANDIDATES = [
    "/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman.ttf",
    "/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman_Bold.ttf",
    "/usr/share/fonts/truetype/liberation2/LiberationSerif-Regular.ttf",
    "/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf",
]
BOLD_CANDIDATES = [
    "/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman_Bold.ttf",
    "/usr/share/fonts/truetype/liberation2/LiberationSerif-Bold.ttf",
    "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf",
]

def first_existing(paths):
    for p in paths:
        if Path(p).exists():
            return p
    raise RuntimeError(f"No usable serif font found: {paths}")

def parse_source(text):
    actors = dict(re.findall(r'^actor\s+"([^"]+)"\s+as\s+(\w+)\s*$', text, re.M))
    # invert name->alias to alias->name
    actors = {alias: name for name, alias in actors.items()}

    ucs = {}
    for label, alias in re.findall(r'^\s*usecase\s+"((?:[^"\\]|\\.)*)"\s+as\s+(\w+)\s*$', text, re.M):
        ucs[alias] = label

    assocs = set()
    for a, op, b in re.findall(r'^\s*(\w+)\s+(-right-|--|-left-|-[^-\s]+-)\s+(\w+)\s*$', text, re.M):
        if "hidden" in op:
            continue
        if a in actors or a in ucs:
            if b in actors or b in ucs:
                assocs.add(frozenset((a, b)))
    return actors, ucs, assocs

def verify_semantics():
    text = PUML.read_text(encoding="utf-8")
    actors, ucs, assocs = parse_source(text)
    if actors != EXPECTED_ACTORS:
        raise SystemExit(f"Hinh 2.9 actors changed. Expected {EXPECTED_ACTORS}, got {actors}")
    if ucs != EXPECTED_UCS:
        raise SystemExit(f"Hinh 2.9 UCs changed. Expected {EXPECTED_UCS}, got {ucs}")
    if assocs != EXPECTED_ASSOCIATIONS:
        raise SystemExit(f"Hinh 2.9 associations changed. Expected {EXPECTED_ASSOCIATIONS}, got {assocs}")
    return actors, ucs

def actor_anchor(alias, actor_xy):
    cx, cy = actor_xy
    if alias in ("Customer", "Reception"):
        return (cx + 30, cy - 5)
    return (cx - 30, cy - 5)

def ellipse_edge(cx, cy, w, h, toward):
    tx, ty = toward
    dx, dy = tx - cx, ty - cy
    rx, ry = w / 2.0, h / 2.0
    if dx == 0 and dy == 0:
        return (cx, cy)
    scale = 1.0 / math.sqrt((dx * dx) / (rx * rx) + (dy * dy) / (ry * ry))
    return (cx + dx * scale, cy + dy * scale)

def draw_actor(draw, cx, cy, label, font):
    r = 18
    head_y = cy - 55
    draw.ellipse((cx-r, head_y-r, cx+r, head_y+r), outline="black", width=2)
    draw.line((cx, head_y+r, cx, cy+18), fill="black", width=2)
    draw.line((cx-30, cy-5, cx+30, cy-5), fill="black", width=2)
    draw.line((cx, cy+18, cx-28, cy+55), fill="black", width=2)
    draw.line((cx, cy+18, cx+28, cy+55), fill="black", width=2)
    bb = draw.textbbox((0,0), label, font=font)
    draw.text((cx-(bb[2]-bb[0])/2, cy+67), label, font=font, fill="black")

def draw_uc(draw, alias, label, font):
    cx, cy, w, h = UC_POS[alias]
    draw.ellipse((cx-w/2, cy-h/2, cx+w/2, cy+h/2), outline="black", width=2)
    lines = label.replace("\\n", "\n").split("\n")
    line_h = 34
    y = cy - line_h * len(lines) / 2
    for line in lines:
        bb = draw.textbbox((0,0), line, font=font)
        draw.text((cx-(bb[2]-bb[0])/2, y), line, font=font, fill="black")
        y += line_h

def connect(draw, actor_alias, uc_alias):
    a = actor_anchor(actor_alias, ACTOR_POS[actor_alias])
    cx, cy, w, h = UC_POS[uc_alias]
    e = ellipse_edge(cx, cy, w, h, a)
    draw.line((a[0], a[1], e[0], e[1]), fill="black", width=2)

def main():
    actors, ucs = verify_semantics()

    regular = first_existing(FONT_CANDIDATES)
    bold = first_existing(BOLD_CANDIDATES)
    f_uc = ImageFont.truetype(regular, 30)
    f_actor = ImageFont.truetype(regular, 30)
    f_title = ImageFont.truetype(bold, 34)

    img = Image.new("RGB", (W, H), "white")
    draw = ImageDraw.Draw(img)

    x1, y1, x2, y2 = BOUNDARY
    draw.rectangle((x1, y1, x2, y2), outline="black", width=3)

    title = "HỆ THỐNG QUẢN LÝ TIÊM CHỦNG"
    bb = draw.textbbox((0,0), title, font=f_title)
    draw.text(((x1+x2-(bb[2]-bb[0]))/2, 150), title, font=f_title, fill="black")

    for alias, label in actors.items():
        draw_actor(draw, *ACTOR_POS[alias], label, f_actor)

    for alias, label in ucs.items():
        draw_uc(draw, alias, label, f_uc)

    for pair in EXPECTED_ASSOCIATIONS:
        a, b = tuple(pair)
        if a in EXPECTED_ACTORS:
            actor_alias, uc_alias = a, b
        else:
            actor_alias, uc_alias = b, a
        connect(draw, actor_alias, uc_alias)

    # Final deterministic print-size target: 180 dpi.
    img.save(PNG, dpi=(180, 180))
    print(f"postprocessed {PNG.name}: {img.size}, font={regular}")

if __name__ == "__main__":
    main()
