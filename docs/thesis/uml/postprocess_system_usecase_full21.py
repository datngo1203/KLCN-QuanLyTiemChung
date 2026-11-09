from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math
import re
import textwrap

ROOT = Path(__file__).resolve().parent
PUML = ROOT / "Hinh_System_Use_Case_21UC_Full_Portrait.puml"
PNG = ROOT / "Hinh_System_Use_Case_21UC_Full_Portrait.png"

W, H = 1900, 3000
BOUNDARY = (250, 280, 1380, 2670)

EXPECTED_ACTORS = {
    "User": "Người dùng",
    "Customer": "Khách hàng",
    "Admin": "Quản trị viên",
    "Reception": "Lễ tân",
    "Doctor": "Bác sĩ",
    "Cashier": "Thu ngân",
    "Nurse": "Điều dưỡng",
    "Warehouse": "Nhân viên kho",
    "Time": "Thời gian",
    "Email": "Dịch vụ thư điện tử",
}

EXPECTED_UCS = {
    "UC01": "Đăng ký tài khoản khách hàng",
    "UC02": "Đăng nhập hệ thống",
    "UC03": "Đổi mật khẩu",
    "UC04": "Đăng xuất hệ thống",
    "UC05": "Thay đổi chức danh tài khoản nhân viên",
    "UC06": "Quản lý danh mục tiêm chủng",
    "UC07": "Quản lý ưu đãi",
    "UC08": "Quản lý lô và tồn kho vắc xin",
    "UC09": "Quản lý hồ sơ khách hàng/người được tiêm",
    "UC10": "Tra cứu dịch vụ tiêm chủng",
    "UC11": "Đăng ký dịch vụ tiêm chủng",
    "UC12": "Quản lý lịch hẹn",
    "UC13": "Gửi nhắc lịch",
    "UC14": "Tự động gửi nhắc lịch",
    "UC15": "Ghi nhận khám sàng lọc và chỉ định",
    "UC16": "Thanh toán dịch vụ",
    "UC17": "Ghi nhận mũi tiêm",
    "UC18": "Ghi nhận theo dõi sau tiêm",
    "UC19": "Theo dõi hành trình tiêm chủng",
    "UC20": "Tra cứu lịch sử tiêm chủng",
    "UC21": "Báo cáo hoạt động",
}

EXPECTED_ASSOCIATIONS = {
    ("User", "UC02"), ("User", "UC03"), ("User", "UC04"),
    ("Customer", "UC01"), ("Customer", "UC09"), ("Customer", "UC10"),
    ("Customer", "UC11"), ("Customer", "UC12"), ("Customer", "UC16"),
    ("Customer", "UC19"), ("Customer", "UC20"),
    ("Reception", "UC09"), ("Reception", "UC11"), ("Reception", "UC12"),
    ("Reception", "UC13"),
    ("Doctor", "UC15"),
    ("Cashier", "UC16"),
    ("Nurse", "UC17"), ("Nurse", "UC18"),
    ("Warehouse", "UC08"),
    ("Admin", "UC05"), ("Admin", "UC06"), ("Admin", "UC07"), ("Admin", "UC21"),
    ("Time", "UC14"),
    ("Email", "UC13"), ("Email", "UC14"),
}

# Presentation-only geometry. The underlying semantic model still keeps
# 8 actor generalizations in SCOPE/detail views. This overview intentionally
# omits that hierarchy to keep the 21-UC direct-association layer readable.
UC_POS = {
    "UC02": (480, 450), "UC03": (800, 450), "UC04": (1120, 450),
    "UC01": (470, 650), "UC05": (1130, 650),
    "UC10": (470, 850), "UC06": (1130, 820),
    "UC09": (800, 1050), "UC07": (1130, 990),
    "UC11": (800, 1250), "UC21": (1130, 1160),
    "UC19": (610, 1380), "UC12": (950, 1500),
    "UC20": (400, 1600), "UC13": (900, 1650),
    "UC15": (1130, 1850), "UC16": (800, 1970),
    "UC17": (1130, 2100), "UC18": (1130, 2220),
    "UC14": (800, 2400), "UC08": (1130, 2580),
}

ACTOR_POS = {
    "User": (1400, 100),
    "Customer": (160, 950),
    "Admin": (1500, 850),
    "Reception": (1500, 1500),
    "Doctor": (1500, 1850),
    "Cashier": (1500, 1970),
    "Nurse": (1500, 2160),
    "Warehouse": (1500, 2580),
    "Time": (650, 2820),
    "Email": (980, 2820),
}

FONT_CANDIDATES = [
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
    actors = {alias: name for name, alias in re.findall(
        r'^actor\s+"([^"]+)"\s+as\s+(\w+)\s*$', text, re.M)}

    ucs = {}
    for label, alias in re.findall(
        r'^\s*usecase\s+"((?:[^"\\]|\\.)*)"\s+as\s+(UC\d+)\s*$', text, re.M):
        plain = label.replace("\\n", " ")
        plain = re.sub(r'^UC\d+\s+', '', plain).strip()
        plain = plain.replace('/ ', '/').replace('  ', ' ')
        ucs[alias] = plain

    parts = re.split(
        r"^'[^\n]*27 direct actor–UC associations[^\n]*$",
        text, maxsplit=1, flags=re.M)
    if len(parts) != 2:
        raise SystemExit("Cannot find 27 direct actor–UC association section in PUML")

    assocs = set()
    for line in parts[1].splitlines():
        line = line.strip()
        if not line or line.startswith("'") or line.startswith("@"):
            continue
        m = re.match(r'^(\w+)\s+(?:--|-right-|-left-|-up-|-down-)\s+(\w+)\s*$', line)
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
        raise SystemExit(f"Full21 overview actor set changed: expected={EXPECTED_ACTORS}, got={actors}")
    if ucs != EXPECTED_UCS:
        raise SystemExit(f"Full21 overview UC set changed: expected={EXPECTED_UCS}, got={ucs}")
    if assocs != EXPECTED_ASSOCIATIONS:
        raise SystemExit(
            f"Full21 overview associations changed: "
            f"missing={EXPECTED_ASSOCIATIONS-assocs}, extra={assocs-EXPECTED_ASSOCIATIONS}")
    active = "\n".join(line for line in text.splitlines() if not line.lstrip().startswith("'"))
    if "<<include>>" in active or "<<extend>>" in active:
        raise SystemExit("Full21 overview must not introduce include/extend")
    if "|>" in active:
        raise SystemExit("Actor generalization is intentionally omitted from this overview view")
    return actors, ucs

def uc_size(alias):
    n = len(EXPECTED_UCS[alias])
    if n >= 38:
        return (330, 118)
    if n >= 29:
        return (300, 108)
    if n >= 21:
        return (270, 100)
    return (235, 92)

def actor_anchor(actor, uc=None):
    x, y = ACTOR_POS[actor]
    if actor == "User":
        return (x, y + 46)
    if actor == "Customer":
        return (x + 29, y - 5)
    if actor in {"Admin", "Reception", "Doctor", "Cashier", "Nurse", "Warehouse"}:
        return (x - 29, y - 5)
    if actor == "Time":
        return (x, y - 62)
    if actor == "Email":
        return (960, 2758) if uc == "UC14" else (1000, 2758)
    return (x, y)

def ellipse_edge_toward(alias, toward):
    cx, cy = UC_POS[alias]
    w, h = uc_size(alias)
    rx, ry = w / 2.0, h / 2.0
    dx, dy = toward[0] - cx, toward[1] - cy
    scale = 1.0 / math.sqrt((dx * dx) / (rx * rx) + (dy * dy) / (ry * ry))
    return (cx + dx * scale, cy + dy * scale)

def ellipse_edge_at_y(alias, y, side="right"):
    cx, cy = UC_POS[alias]
    w, h = uc_size(alias)
    rx, ry = w / 2.0, h / 2.0
    q = (y - cy) / ry
    if abs(q) >= 1:
        raise RuntimeError(f"Requested y={y} is outside ellipse {alias}")
    dx = rx * math.sqrt(1 - q * q)
    return (cx + dx if side == "right" else cx - dx, y)

def build_paths():
    paths = {}
    for actor, uc in sorted(EXPECTED_ASSOCIATIONS):
        start = actor_anchor(actor, uc)
        if (actor, uc) == ("Email", "UC13"):
            # Only non-straight direct association in the full-21 overview.
            # Two 90-degree elbows route outside the right-side actor column,
            # then enter the lower-right quadrant of UC13.
            paths[(actor, uc)] = [
                start,
                (1760, start[1]),
                (1760, 1680),
                ellipse_edge_at_y("UC13", 1680, "right"),
            ]
        else:
            paths[(actor, uc)] = [start, ellipse_edge_toward(uc, start)]
    return paths

def segment_intersects_ellipse(p0, p1, alias, margin=4):
    if p0 == p1:
        return False
    cx, cy = UC_POS[alias]
    w, h = uc_size(alias)
    rx, ry = w / 2.0 + margin, h / 2.0 + margin
    x0, y0 = (p0[0] - cx) / rx, (p0[1] - cy) / ry
    x1, y1 = (p1[0] - cx) / rx, (p1[1] - cy) / ry
    dx, dy = x1 - x0, y1 - y0
    aa = dx * dx + dy * dy
    bb = 2 * (x0 * dx + y0 * dy)
    cc = x0 * x0 + y0 * y0 - 1
    disc = bb * bb - 4 * aa * cc
    if disc < 0:
        return cc < 0
    sd = math.sqrt(max(0, disc))
    t1 = (-bb - sd) / (2 * aa)
    t2 = (-bb + sd) / (2 * aa)
    return cc < 0 or (1e-4 < t1 < 1 - 1e-4) or (1e-4 < t2 < 1 - 1e-4)

def orient(a, b, c):
    return (b[0]-a[0]) * (c[1]-a[1]) - (b[1]-a[1]) * (c[0]-a[0])

def proper_cross(a, b, c, d):
    o1, o2, o3, o4 = orient(a, b, c), orient(a, b, d), orient(c, d, a), orient(c, d, b)
    return o1 * o2 < 0 and o3 * o4 < 0

def rect_hit_segment(rect, p0, p1):
    x1, y1, x2, y2 = rect
    def inside(p):
        return x1 <= p[0] <= x2 and y1 <= p[1] <= y2
    if inside(p0) or inside(p1):
        return True
    edges = [
        ((x1, y1), (x2, y1)),
        ((x2, y1), (x2, y2)),
        ((x2, y2), (x1, y2)),
        ((x1, y2), (x1, y1)),
    ]
    return any(proper_cross(p0, p1, a, b) for a, b in edges)

def actor_label_boxes(draw, font):
    boxes = {}
    for actor, (x, y) in ACTOR_POS.items():
        name = EXPECTED_ACTORS[actor]
        if actor == "User":
            bb = draw.textbbox((0, 0), name, font=font)
            w, h = bb[2]-bb[0], bb[3]-bb[1]
            boxes[actor] = (x-w/2-5, y-110, x+w/2+5, y-110+h+10)
        elif actor == "Customer":
            bb = draw.textbbox((0, 0), name, font=font)
            w, h = bb[2]-bb[0], bb[3]-bb[1]
            boxes[actor] = (85-w/2-5, y-23, 85+w/2+5, y-23+h+10)
        elif actor in {"Admin", "Reception", "Doctor", "Cashier", "Nurse", "Warehouse"}:
            lines = textwrap.wrap(name, width=13, break_long_words=False)
            widths = [draw.textbbox((0,0), line, font=font)[2] for line in lines]
            boxes[actor] = (x+37, y-20, x+52+max(widths), y-20+32*len(lines))
        else:
            lines = textwrap.wrap(name, width=16, break_long_words=False)
            widths = [draw.textbbox((0,0), line, font=font)[2] for line in lines]
            boxes[actor] = (x-max(widths)/2-5, y+53, x+max(widths)/2+5, y+68+31*len(lines))
    return boxes

def audit_geometry(paths, draw, actor_font, title_font):
    if len(paths) != 27:
        raise SystemExit(f"Expected 27 routed associations, got {len(paths)}")

    straight = sum(1 for p in paths.values() if len(p) == 2)
    orthogonal = len(paths) - straight
    if straight != 26 or orthogonal != 1:
        raise SystemExit(f"Straight-first contract broken: straight={straight}, orthogonal={orthogonal}")
    if len(paths[("Email", "UC13")]) != 4:
        raise SystemExit("Email→UC13 must remain the sole 2-elbow orthogonal relation")

    for key, path in paths.items():
        target = key[1]
        for p0, p1 in zip(path, path[1:]):
            for other in UC_POS:
                if other == target:
                    continue
                if segment_intersects_ellipse(p0, p1, other):
                    raise SystemExit(f"{key} intrudes into {other}: {p0}->{p1}")

    items = list(paths.items())
    for i, (k1, p1) in enumerate(items):
        for k2, p2 in items[i+1:]:
            if k1[0] == k2[0]:
                continue
            for a, b in zip(p1, p1[1:]):
                for c, d in zip(p2, p2[1:]):
                    if proper_cross(a, b, c, d):
                        raise SystemExit(f"Association crossing: {k1} x {k2}")

    labels = actor_label_boxes(draw, actor_font)
    for key, path in paths.items():
        for p0, p1 in zip(path, path[1:]):
            for actor, rect in labels.items():
                if rect_hit_segment(rect, p0, p1):
                    raise SystemExit(f"{key} crosses actor label {actor}")

    title = "HỆ THỐNG QUẢN LÝ TIÊM CHỦNG"
    bb = draw.textbbox((BOUNDARY[0]+22, BOUNDARY[1]+17), title, font=title_font)
    title_safe = (bb[0]-8, bb[1]-8, bb[2]+8, bb[3]+8)
    for key, path in paths.items():
        for p0, p1 in zip(path, path[1:]):
            if rect_hit_segment(title_safe, p0, p1):
                raise SystemExit(f"{key} crosses title safe zone")

    print("geometry audit PASS: 26 straight / 1 orthogonal, crossings=0, ellipse intrusion=0, label/title intrusion=0")

def wrap_text(draw, text, font, max_width):
    words = text.replace("/", "/ ").split()
    lines, current = [], ""
    for word in words:
        candidate = (current + " " + word).strip().replace("/ ", "/")
        bb = draw.textbbox((0, 0), candidate, font=font)
        if not current or bb[2] - bb[0] <= max_width:
            current = candidate
        else:
            lines.append(current)
            current = word.replace("/ ", "/")
    if current:
        lines.append(current)
    return lines

def draw_uc(draw, alias, font, bold):
    cx, cy = UC_POS[alias]
    w, h = uc_size(alias)
    draw.ellipse((cx-w/2, cy-h/2, cx+w/2, cy+h/2), outline="black", width=3)
    lines = wrap_text(draw, EXPECTED_UCS[alias], font, w-30)
    y = cy - (30 + 29*len(lines))/2 + 3
    bb = draw.textbbox((0,0), alias, font=bold)
    draw.text((cx-(bb[2]-bb[0])/2, y), alias, font=bold, fill="black")
    y += 31
    for line in lines:
        bb = draw.textbbox((0,0), line, font=font)
        draw.text((cx-(bb[2]-bb[0])/2, y), line, font=font, fill="black")
        y += 29

def draw_actor(draw, alias, font):
    x, y = ACTOR_POS[alias]
    r, head_y = 14, y - 46
    draw.ellipse((x-r, head_y-r, x+r, head_y+r), outline="black", width=3)
    draw.line((x, head_y+r, x, y+12), fill="black", width=3)
    draw.line((x-24, y-6, x+24, y-6), fill="black", width=3)
    draw.line((x, y+12, x-22, y+42), fill="black", width=3)
    draw.line((x, y+12, x+22, y+42), fill="black", width=3)

    name = EXPECTED_ACTORS[alias]
    if alias == "User":
        bb = draw.textbbox((0,0), name, font=font)
        draw.text((x-(bb[2]-bb[0])/2, y-105), name, font=font, fill="black")
    elif alias == "Customer":
        bb = draw.textbbox((0,0), name, font=font)
        draw.text((85-(bb[2]-bb[0])/2, y-18), name, font=font, fill="black")
    elif alias in {"Admin", "Reception", "Doctor", "Cashier", "Nurse", "Warehouse"}:
        yy = y - 15
        for line in textwrap.wrap(name, width=13, break_long_words=False):
            draw.text((x+42, yy), line, font=font, fill="black")
            yy += 30
    else:
        yy = y + 58
        for line in textwrap.wrap(name, width=16, break_long_words=False):
            bb = draw.textbbox((0,0), line, font=font)
            draw.text((x-(bb[2]-bb[0])/2, yy), line, font=font, fill="black")
            yy += 30

def main():
    verify_semantics()

    regular = first_existing(FONT_CANDIDATES)
    bold_path = first_existing(BOLD_CANDIDATES)
    f_uc = ImageFont.truetype(regular, 29)
    f_uc_bold = ImageFont.truetype(bold_path, 29)
    f_actor = ImageFont.truetype(regular, 27)
    f_title = ImageFont.truetype(bold_path, 31)

    img = Image.new("RGB", (W, H), "white")
    draw = ImageDraw.Draw(img)
    paths = build_paths()
    audit_geometry(paths, draw, f_actor, f_title)

    for path in paths.values():
        draw.line(path, fill="black", width=3, joint="curve")

    x1, y1, x2, y2 = BOUNDARY
    draw.rectangle(BOUNDARY, outline="black", width=3)

    title = "HỆ THỐNG QUẢN LÝ TIÊM CHỦNG"
    title_bb = draw.textbbox((x1+22, y1+17), title, font=f_title)
    draw.rectangle((x1+10, y1+5, title_bb[2]+10, y1+62), fill="white")
    draw.text((x1+22, y1+17), title, font=f_title, fill="black")

    for alias in UC_POS:
        draw_uc(draw, alias, f_uc, f_uc_bold)
    for alias in ACTOR_POS:
        draw_actor(draw, alias, f_actor)

    img.save(PNG, dpi=(180, 180))
    print(f"postprocessed {PNG.name}: {img.size}, font={regular}")
    print("verified presentation: 21 UC / 10 visible actors / 27 direct associations")
    print("actor hierarchy remains semantic/detail-view scope per SCOPE.md")

if __name__ == "__main__":
    main()
