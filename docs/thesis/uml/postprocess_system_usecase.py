from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import itertools
import math
import re

ROOT = Path(__file__).resolve().parent
PUML = ROOT / "Hinh_2_9_System_Use_Case_Tong_Quan.puml"
PNG = ROOT / "Hinh_2_9_System_Use_Case_Tong_Quan.png"

# Deterministic geometry inherited from the user-approved run #58 layout.
# Elements no longer in the final 14-UC thesis model are removed; remaining
# approved coordinates are preserved to minimize visual regression.
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
}

EXPECTED_UCS = {
    "UC06": "Quản lý danh mục\\ntiêm chủng",
    "UC07": "Quản lý ưu đãi",
    "UC08": "Quản lý lô và tồn kho\\nvắc xin",
    "UC09": "Quản lý hồ sơ khách hàng/\\nngười được tiêm",
    "UC10": "Tra cứu dịch vụ\\ntiêm chủng",
    "UC11": "Đăng ký dịch vụ\\ntiêm chủng",
    "UC12": "Quản lý lịch hẹn",
    "UC13": "Gửi nhắc lịch",
    "UC15": "Ghi nhận khám sàng lọc\\nvà chỉ định",
    "UC16": "Thanh toán dịch vụ",
    "UC17": "Ghi nhận mũi tiêm",
    "UC18": "Ghi nhận theo dõi\\nsau tiêm",
    "UC19": "Theo dõi hành trình\\ntiêm chủng",
    "UC21": "Báo cáo hoạt động",
}

EXPECTED_ASSOCIATIONS = {
    ("Customer","UC09"), ("Customer","UC10"), ("Customer","UC11"),
    ("Customer","UC12"), ("Customer","UC16"), ("Customer","UC19"),
    ("Reception","UC09"), ("Reception","UC11"), ("Reception","UC12"),
    ("Reception","UC13"),
    ("Admin","UC06"), ("Admin","UC07"), ("Admin","UC21"),
    ("Cashier","UC16"), ("Doctor","UC15"),
    ("Nurse","UC17"), ("Nurse","UC18"),
    ("Warehouse","UC08"),
    ("Email","UC13"),
}

# Exact remaining coordinates from the human-approved run #58 geometry.
UC_BOX = {
    "UC06": (890, 690, 270, 100),
    "UC07": (990, 830, 235, 92),
    "UC08": (940, 2042, 300, 108),
    "UC09": (622.5, 890, 330, 118),
    "UC10": (382, 775, 270, 100),
    "UC11": (652.5, 1080, 270, 100),
    "UC12": (670, 1250, 235, 92),
    "UC13": (687.5, 1630, 235, 92),
    "UC15": (940, 1656, 300, 108),
    "UC16": (670, 1400, 235, 92),
    "UC17": (972.5, 1796, 235, 92),
    "UC18": (952.5, 1915, 270, 100),
    "UC19": (460, 1510, 270, 100),
    "UC21": (1087, 960, 235, 92),
}

ACTOR_BOX = {
    "Customer": (120, 1052, 60, 110),
    "Admin": (1430, 765, 60, 110),
    "Reception": (1430, 1120, 60, 110),
    "Cashier": (1430, 1400, 60, 110),
    "Doctor": (1430, 1655, 60, 110),
    "Nurse": (1430, 1831, 60, 110),
    "Warehouse": (1430, 2040, 60, 110),
    "Email": (120, 1905, 60, 110),
}

# One crossing from the user-approved geometry is intentionally preserved.
ACCEPTED_CROSSING = frozenset({
    ("Reception", "UC13"),
    ("Cashier", "UC16"),
})

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
        r"^'\s*19 direct actor–UC associations\.\s*$",
        text,
        maxsplit=1,
        flags=re.M,
    )
    if len(parts) != 2:
        raise SystemExit("Cannot find 19 direct actor–UC association section")

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
    forbidden = {"UC01", "UC02", "UC03", "UC04", "UC05", "UC14", "UC20"}
    if forbidden & set(ucs):
        raise SystemExit(f"supporting UC leaked into final overview: {forbidden & set(ucs)}")
    if "Time" in actors:
        raise SystemExit("Time actor leaked into overview after UC14 became supporting")
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
    cx, _ = center(ACTOR_BOX[alias])
    tx, _ = target
    if tx >= cx:
        return x + w, y + 50
    return x, y + 50


def association_segment(actor, uc):
    uc_center = center(UC_BOX[uc])
    start = actor_anchor(actor, uc_center)
    end = ellipse_edge(UC_BOX[uc], start)
    return start, end


def segment_hits_ellipse(p1, p2, box):
    x, y, w, h = box
    cx, cy = center(box)
    rx, ry = w / 2.0, h / 2.0
    x1, y1 = (p1[0] - cx) / rx, (p1[1] - cy) / ry
    x2, y2 = (p2[0] - cx) / rx, (p2[1] - cy) / ry
    dx, dy = x2 - x1, y2 - y1
    a = dx * dx + dy * dy
    if a == 0:
        return False
    b = 2 * (x1 * dx + y1 * dy)
    c = x1 * x1 + y1 * y1 - 1
    disc = b * b - 4 * a * c
    if disc < 0:
        return False
    root = math.sqrt(max(0, disc))
    t1 = (-b - root) / (2 * a)
    t2 = (-b + root) / (2 * a)
    return any(0.001 < t < 0.999 for t in (t1, t2))


def orient(a, b, c):
    return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])


def proper_cross(s1, s2):
    a, b = s1
    c, d = s2
    o1, o2 = orient(a, b, c), orient(a, b, d)
    o3, o4 = orient(c, d, a), orient(c, d, b)
    return o1 * o2 < 0 and o3 * o4 < 0


def verify_geometry():
    segments = {
        pair: association_segment(*pair)
        for pair in EXPECTED_ASSOCIATIONS
    }

    intrusions = []
    for (actor, uc), segment in segments.items():
        for other_uc, box in UC_BOX.items():
            if other_uc == uc:
                continue
            if segment_hits_ellipse(*segment, box):
                intrusions.append((actor, uc, other_uc))
    if intrusions:
        raise SystemExit(f"connector-through-ellipse regression: {intrusions}")

    crossings = []
    for (pair1, seg1), (pair2, seg2) in itertools.combinations(segments.items(), 2):
        if pair1[0] == pair2[0] or pair1[1] == pair2[1]:
            continue
        if proper_cross(seg1, seg2):
            crossings.append(frozenset({pair1, pair2}))

    if crossings != [ACCEPTED_CROSSING]:
        raise SystemExit(
            f"crossing regression: expected={[ACCEPTED_CROSSING]} actual={crossings}"
        )
    return len(crossings)


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
    start, end = association_segment(actor, uc)
    draw.line((start[0], start[1], end[0], end[1]), fill="black", width=2)


def main():
    verify_semantics()
    crossing_count = verify_geometry()

    regular = first_existing(REGULAR_CANDIDATES)
    bold = first_existing(BOLD_CANDIDATES)
    f_uc = ImageFont.truetype(regular, 24)
    f_actor = ImageFont.truetype(regular, 23)
    f_title = ImageFont.truetype(bold, 27)

    img = Image.new("RGB", (W, H), "white")
    draw = ImageDraw.Draw(img)

    for actor, uc in sorted(EXPECTED_ASSOCIATIONS):
        draw_association(draw, actor, uc)

    x1, y1, x2, y2 = BOUNDARY
    draw.rectangle((x1, y1, x2, y2), outline="black", width=2)

    title = "HỆ THỐNG QUẢN LÝ TIÊM CHỦNG"
    draw.rectangle((x1+10, y1+6, x1+520, y1+42), fill="white")
    draw.text((x1+18, y1+10), title, font=f_title, fill="black")

    for alias in UC_BOX:
        draw_uc(draw, alias, f_uc)
    for alias in ACTOR_BOX:
        draw_actor(draw, alias, f_actor)

    img.save(PNG, dpi=(180, 180))
    print(f"postprocessed {PNG.name}: {img.size}, font={regular}")
    print("verified: 14 visible UC / 8 actors / 19 associations")
    print("geometry: connector-through-ellipse=0")
    print(f"geometry: crossings={crossing_count} (accepted run #58 pair only)")


if __name__ == "__main__":
    main()
