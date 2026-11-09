from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math
import re

ROOT = Path(__file__).resolve().parent
PUML = ROOT / "Hinh_2_9_System_Use_Case_Tong_Quan.puml"
PNG = ROOT / "Hinh_2_9_System_Use_Case_Tong_Quan.png"

# Deterministic geometry copied from the human-approved draw.io layout.
# PlantUML remains the canonical semantic source; this script only stabilizes
# the final thesis geometry so CI does not reflow the diagram unpredictably.
W, H = 1620, 2380
BOUNDARY = (240, 510, 1370, 2250)

EXPECTED_ACTORS = {
    "Customer": "Khách hàng",
    "Admin": "Quản trị viên",
    "Reception": "Lễ tân",
    "Cashier": "Thu ngân",
    "Doctor": "Bác sĩ",
    "Nurse": "Điều dưỡng",
    "Warehouse": "Nhân viên kho",
    "Email": "Dịch vụ thư điện tử",
    "Time": "Thời gian",
}

EXPECTED_UCS = {
    "UC01": "Đăng ký tài khoản\\nkhách hàng",
    "UC05": "Thay đổi chức danh\\ntài khoản nhân viên",
    "UC06": "Quản lý danh mục\\ntiêm chủng",
    "UC07": "Quản lý ưu đãi",
    "UC08": "Quản lý lô và tồn kho\\nvắc xin",
    "UC09": "Quản lý hồ sơ khách hàng/\\nngười được tiêm",
    "UC10": "Tra cứu dịch vụ\\ntiêm chủng",
    "UC11": "Đăng ký dịch vụ\\ntiêm chủng",
    "UC12": "Quản lý lịch hẹn",
    "UC13": "Gửi nhắc lịch",
    "UC14": "Tự động gửi nhắc lịch",
    "UC15": "Ghi nhận khám sàng lọc\\nvà chỉ định",
    "UC16": "Thanh toán dịch vụ",
    "UC17": "Ghi nhận mũi tiêm",
    "UC18": "Ghi nhận theo dõi\\nsau tiêm",
    "UC19": "Theo dõi hành trình\\ntiêm chủng",
    "UC20": "Tra cứu lịch sử\\ntiêm chủng",
    "UC21": "Báo cáo hoạt động",
}

EXPECTED_ASSOCIATIONS = {
    ("Customer","UC01"), ("Customer","UC09"), ("Customer","UC10"),
    ("Customer","UC11"), ("Customer","UC12"), ("Customer","UC16"),
    ("Customer","UC19"), ("Customer","UC20"),
    ("Reception","UC09"), ("Reception","UC11"), ("Reception","UC12"),
    ("Reception","UC13"),
    ("Admin","UC05"), ("Admin","UC06"), ("Admin","UC07"), ("Admin","UC21"),
    ("Cashier","UC16"), ("Doctor","UC15"),
    ("Nurse","UC17"), ("Nurse","UC18"),
    ("Warehouse","UC08"),
    ("Email","UC13"), ("Email","UC14"), ("Time","UC14"),
}

# x, y, width, height — exact user-edited draw.io geometry.
UC_BOX = {
    "UC01": (330, 580, 270, 100),
    "UC05": (1010, 540, 300, 108),
    "UC06": (890, 690, 270, 100),
    "UC07": (990, 830, 235, 92),
    "UC08": (940, 2042, 300, 108),
    "UC09": (622.5, 890, 330, 118),
    "UC10": (382, 775, 270, 100),
    "UC11": (652.5, 1080, 270, 100),
    "UC12": (670, 1250, 235, 92),
    "UC13": (687.5, 1630, 235, 92),
    "UC14": (622.5, 1850, 270, 100),
    "UC15": (940, 1656, 300, 108),
    "UC16": (670, 1400, 235, 92),
    "UC17": (972.5, 1796, 235, 92),
    "UC18": (952.5, 1915, 270, 100),
    "UC19": (460, 1510, 270, 100),
    "UC20": (352.5, 1660, 270, 100),
    "UC21": (1087, 960, 235, 92),
}

# x, y, width, height — exact user-edited draw.io actor boxes.
ACTOR_BOX = {
    "Customer": (120, 1052, 60, 110),
    "Admin": (1430, 765, 60, 110),
    "Reception": (1430, 1120, 60, 110),
    "Cashier": (1430, 1400, 60, 110),
    "Doctor": (1430, 1655, 60, 110),
    "Nurse": (1430, 1831, 60, 110),
    "Warehouse": (1430, 2040, 60, 110),
    "Time": (120, 2100, 60, 110),
    "Email": (120, 1905, 60, 110),
}

REGULAR_CANDIDATES = [
    "/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman.ttf",
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
    actors = {
        alias: name
        for name, alias in re.findall(
            r'^actor\s+"([^"]+)"\s+as\s+(\w+)\s*$', text, re.M
        )
    }
    ucs = {}
    for label, alias in re.findall(
        r'^\s*usecase\s+"((?:[^"\\]|\\.)*)"\s+as\s+(UC\d+)\s*$',
        text,
        re.M,
    ):
        ucs[alias] = label

    assocs = set()
    parts = re.split(
        r"^'\s*24 direct actor–UC associations\.\s*$",
        text,
        maxsplit=1,
        flags=re.M,
    )
    if len(parts) != 2:
        raise SystemExit("Cannot find 24 direct actor–UC association section")

    for line in parts[1].splitlines():
        line = line.strip()
        if not line or line.startswith("'") or line.startswith("@"):
            continue
        m = re.match(
            r'^(\w+)\s+(?:--|-right-|-left-|-up-|-down-)\s+(\w+)\s*$',
            line,
        )
        if not m:
            continue
        a, b = m.groups()
        if a in actors and b in ucs:
            assocs.add((a, b))
        elif b in actors and a in ucs:
            assocs.add((b, a))
    return actors, ucs, assocs


def verify_semantics():
    text = PUML.read_text(encoding="utf-8")
    actors, ucs, assocs = parse_source(text)
    if actors != EXPECTED_ACTORS:
        raise SystemExit(f"actor mismatch: {actors}")
    if ucs != EXPECTED_UCS:
        raise SystemExit(f"UC mismatch: {ucs}")
    if assocs != EXPECTED_ASSOCIATIONS:
        raise SystemExit(
            f"association mismatch missing={EXPECTED_ASSOCIATIONS-assocs} "
            f"extra={assocs-EXPECTED_ASSOCIATIONS}"
        )
    if {"UC02", "UC03", "UC04"} & set(ucs):
        raise SystemExit("authentication UC leaked into overview")
    if re.search(r'<<\s*(include|extend)\s*>>', text, re.I):
        raise SystemExit("include/extend must not appear in Hình 2.9")
    if "|>" in text:
        raise SystemExit("actor/UC generalization must not appear in Hình 2.9")


def center(box):
    x, y, w, h = box
    return x + w / 2.0, y + h / 2.0


def ellipse_edge(box, toward):
    x, y, w, h = box
    cx, cy = center(box)
    tx, ty = toward
    dx, dy = tx - cx, ty - cy
    rx, ry = w / 2.0, h / 2.0
    if dx == 0 and dy == 0:
        return cx, cy
    scale = 1.0 / math.sqrt((dx * dx) / (rx * rx) + (dy * dy) / (ry * ry))
    return cx + dx * scale, cy + dy * scale


def actor_anchor(alias, target):
    x, y, w, h = ACTOR_BOX[alias]
    cx, cy = center(ACTOR_BOX[alias])
    tx, _ = target
    # Use arm-side anchors so associations remain visually attached.
    if tx >= cx:
        return x + w, y + 50
    return x, y + 50


def draw_actor(draw, alias, font):
    x, y, w, h = ACTOR_BOX[alias]
    cx = x + w / 2.0
    head_y = y + 20
    r = 12
    draw.ellipse((cx-r, head_y-r, cx+r, head_y+r), outline="black", width=2)
    draw.line((cx, head_y+r, cx, y+70), fill="black", width=2)
    draw.line((cx-22, y+48, cx+22, y+48), fill="black", width=2)
    draw.line((cx, y+70, cx-20, y+94), fill="black", width=2)
    draw.line((cx, y+70, cx+20, y+94), fill="black", width=2)

    label = EXPECTED_ACTORS[alias]
    lines = [label]
    if label == "Dịch vụ thư điện tử":
        lines = ["Dịch vụ thư", "điện tử"]
    yy = y + 101
    for line in lines:
        bb = draw.textbbox((0, 0), line, font=font)
        draw.text((cx-(bb[2]-bb[0])/2, yy), line, font=font, fill="black")
        yy += 27


def draw_uc(draw, alias, font):
    x, y, w, h = UC_BOX[alias]
    draw.ellipse((x, y, x+w, y+h), outline="black", width=2)
    lines = EXPECTED_UCS[alias].replace("\\n", "\n").split("\n")
    line_h = 27
    total = line_h * len(lines)
    yy = y + (h-total)/2
    for line in lines:
        bb = draw.textbbox((0, 0), line, font=font)
        draw.text((x+w/2-(bb[2]-bb[0])/2, yy), line, font=font, fill="black")
        yy += line_h


def draw_association(draw, actor, uc):
    uc_center = center(UC_BOX[uc])
    start = actor_anchor(actor, uc_center)
    end = ellipse_edge(UC_BOX[uc], start)
    draw.line((start[0], start[1], end[0], end[1]), fill="black", width=2)


def main():
    verify_semantics()

    regular = first_existing(REGULAR_CANDIDATES)
    bold = first_existing(BOLD_CANDIDATES)
    f_uc = ImageFont.truetype(regular, 24)
    f_actor = ImageFont.truetype(regular, 23)
    f_title = ImageFont.truetype(bold, 27)

    img = Image.new("RGB", (W, H), "white")
    draw = ImageDraw.Draw(img)

    # Connectors first; shapes redraw over endpoints for clean attachment.
    for actor, uc in sorted(EXPECTED_ASSOCIATIONS):
        draw_association(draw, actor, uc)

    x1, y1, x2, y2 = BOUNDARY
    draw.rectangle((x1, y1, x2, y2), outline="black", width=2)

    # PlantUML/report-compatible monochrome title treatment.
    title = "HỆ THỐNG QUẢN LÝ TIÊM CHỦNG"
    draw.rectangle((x1+10, y1+6, x1+520, y1+42), fill="white")
    draw.text((x1+18, y1+10), title, font=f_title, fill="black")

    for alias in UC_BOX:
        draw_uc(draw, alias, f_uc)
    for alias in ACTOR_BOX:
        draw_actor(draw, alias, f_actor)

    img.save(PNG, dpi=(180, 180))
    print(f"postprocessed {PNG.name}: {img.size}, font={regular}")
    print("verified: 18 visible UC / 9 actors / 24 associations")
    print("geometry: exact human-approved draw.io baseline; remaining accepted crossing preserved")


if __name__ == "__main__":
    main()
