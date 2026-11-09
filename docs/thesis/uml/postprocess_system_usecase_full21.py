from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math, re

ROOT = Path(__file__).resolve().parent
PUML = ROOT / 'Hinh_System_Use_Case_21UC_Full_Portrait.puml'
PNG = ROOT / 'Hinh_System_Use_Case_21UC_Full_Portrait.png'

W, H = 1900, 3000
BOUNDARY = (250, 280, 1380, 2670)

# Full-21 overview presentation intentionally omits the Employee general actor
# and all 8 actor-generalization edges. The semantic hierarchy remains locked
# in SCOPE.md and is shown/audited in detail views.
EXPECTED_ACTORS = {
    'User': 'Người dùng',
    'Customer': 'Khách hàng',
    'Admin': 'Quản trị viên',
    'Reception': 'Lễ tân',
    'Doctor': 'Bác sĩ',
    'Cashier': 'Thu ngân',
    'Nurse': 'Điều dưỡng',
    'Warehouse': 'Nhân viên kho',
    'Time': 'Thời gian',
    'Email': 'Dịch vụ thư điện tử',
}
EXPECTED_UCS = {
    'UC01': 'Đăng ký tài khoản khách hàng',
    'UC02': 'Đăng nhập hệ thống',
    'UC03': 'Đổi mật khẩu',
    'UC04': 'Đăng xuất hệ thống',
    'UC05': 'Thay đổi chức danh tài khoản nhân viên',
    'UC06': 'Quản lý danh mục tiêm chủng',
    'UC07': 'Quản lý ưu đãi',
    'UC08': 'Quản lý lô và tồn kho vắc xin',
    'UC09': 'Quản lý hồ sơ khách hàng/người được tiêm',
    'UC10': 'Tra cứu dịch vụ tiêm chủng',
    'UC11': 'Đăng ký dịch vụ tiêm chủng',
    'UC12': 'Quản lý lịch hẹn',
    'UC13': 'Gửi nhắc lịch',
    'UC14': 'Tự động gửi nhắc lịch',
    'UC15': 'Ghi nhận khám sàng lọc và chỉ định',
    'UC16': 'Thanh toán dịch vụ',
    'UC17': 'Ghi nhận mũi tiêm',
    'UC18': 'Ghi nhận theo dõi sau tiêm',
    'UC19': 'Theo dõi hành trình tiêm chủng',
    'UC20': 'Tra cứu lịch sử tiêm chủng',
    'UC21': 'Báo cáo hoạt động',
}
EXPECTED_ASSOC = {
    ('User','UC02'), ('User','UC03'), ('User','UC04'),
    ('Customer','UC01'), ('Customer','UC09'), ('Customer','UC10'),
    ('Customer','UC11'), ('Customer','UC12'), ('Customer','UC16'),
    ('Customer','UC19'), ('Customer','UC20'),
    ('Reception','UC09'), ('Reception','UC11'), ('Reception','UC12'),
    ('Reception','UC13'),
    ('Doctor','UC15'), ('Cashier','UC16'),
    ('Nurse','UC17'), ('Nurse','UC18'),
    ('Warehouse','UC08'),
    ('Admin','UC05'), ('Admin','UC06'), ('Admin','UC07'), ('Admin','UC21'),
    ('Time','UC14'), ('Email','UC13'), ('Email','UC14'),
}

# Straight-first geometry. 25/27 associations are direct straight segments.
# Only Admin->UC21 and Reception->UC13 use orthogonal detours, because the
# direct segments would collide with the central/right routing.
UC_POS = {
    'UC02': (500, 470),  'UC03': (820, 470),  'UC04': (1140, 470),

    'UC01': (470, 650),  'UC05': (1140, 650),
    'UC10': (470, 850),  'UC06': (1140, 850),
    'UC09': (820, 950),  'UC07': (1140, 1050),
    'UC11': (820, 1150),
    'UC12': (820, 1400), 'UC21': (1140, 1550),

    'UC19': (470, 1650), 'UC16': (820, 1700),
    'UC15': (1140, 1850),
    'UC20': (470, 2050), 'UC13': (850, 2050),
    'UC17': (1140, 2150),
    'UC18': (1140, 2350), 'UC14': (650, 2400),
    'UC08': (1140, 2550),
}
ACTOR_POS = {
    'User': (820, 110),
    'Customer': (105, 1100),
    'Admin': (1530, 850),
    'Reception': (1530, 1400),
    'Cashier': (1530, 1700),
    'Doctor': (1530, 1850),
    'Nurse': (1530, 2250),
    'Warehouse': (1530, 2550),
    'Time': (650, 2860),
    'Email': (850, 2860),
}

REGULAR = '/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman.ttf'
BOLD = '/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman_Bold.ttf'
if not Path(REGULAR).exists():
    REGULAR = '/usr/share/fonts/truetype/liberation2/LiberationSerif-Regular.ttf'
if not Path(BOLD).exists():
    BOLD = '/usr/share/fonts/truetype/liberation2/LiberationSerif-Bold.ttf'


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
        plain = label.replace('\\n', ' ')
        plain = re.sub(r'^UC\d+\s+', '', plain).strip()
        ucs[alias] = plain.replace('/ ', '/').replace('  ', ' ')

    # Overview must not accidentally regain actor generalization.
    gens = set()
    for a, b in re.findall(r'^\s*(\w+)\s+-[^\n]*\|>\s+(\w+)\s*$', text, re.M):
        if a in actors and b in actors:
            gens.add((a, b))

    assoc = set()
    parts = re.split(
        r"^'[^\n]*27 direct actor–UC associations[^\n]*$",
        text,
        maxsplit=1,
        flags=re.M,
    )
    if len(parts) != 2:
        raise SystemExit('Cannot find 27 direct actor–UC association section in PUML')

    for line in parts[1].splitlines():
        line = line.strip()
        if not line or line.startswith("'") or line.startswith('@'):
            continue
        m = re.match(
            r'^(\w+)\s+(?:--|-right-|-left-|-up-|-down-)\s+(\w+)\s*$',
            line,
        )
        if not m:
            continue
        a, b = m.groups()
        if a in actors and b in ucs:
            assoc.add((a, b))
        elif b in actors and a in ucs:
            assoc.add((b, a))

    return actors, ucs, assoc, gens


def verify_semantics():
    actors, ucs, assoc, gens = parse_source(PUML.read_text(encoding='utf-8'))
    if actors != EXPECTED_ACTORS:
        raise SystemExit(f'actor semantic mismatch: {actors}')
    if ucs != EXPECTED_UCS:
        raise SystemExit(f'UC semantic mismatch: {ucs}')
    if assoc != EXPECTED_ASSOC:
        raise SystemExit(
            f'association mismatch missing={EXPECTED_ASSOC-assoc} extra={assoc-EXPECTED_ASSOC}'
        )
    if gens:
        raise SystemExit(
            f'full-21 overview must omit actor generalization; found {gens}'
        )


def uc_size(alias):
    n = len(EXPECTED_UCS[alias])
    if n >= 38:
        return 330, 118
    if n >= 29:
        return 300, 108
    if n >= 21:
        return 270, 100
    return 235, 92


def draw_actor(draw, alias, font):
    x, y = ACTOR_POS[alias]
    r = 14
    head_y = y - 46
    draw.ellipse((x-r, head_y-r, x+r, head_y+r), outline='black', width=3)
    draw.line((x, head_y+r, x, y+12), fill='black', width=3)
    draw.line((x-24, y-6, x+24, y-6), fill='black', width=3)
    draw.line((x, y+12, x-22, y+42), fill='black', width=3)
    draw.line((x, y+12, x+22, y+42), fill='black', width=3)

    label = EXPECTED_ACTORS[alias]
    lines = [label]
    if label == 'Dịch vụ thư điện tử':
        lines = ['Dịch vụ thư', 'điện tử']
    yy = y + 58
    for line in lines:
        bb = draw.textbbox((0,0), line, font=font)
        draw.text((x-(bb[2]-bb[0])/2, yy), line, font=font, fill='black')
        yy += 32


def wrap_text(draw, text, font, maxw):
    words = text.replace('/', '/ ').split()
    lines, cur = [], ''
    for w in words:
        test = (cur + ' ' + w).strip().replace('/ ', '/')
        bb = draw.textbbox((0,0), test, font=font)
        if bb[2]-bb[0] <= maxw or not cur:
            cur = test
        else:
            lines.append(cur)
            cur = w.replace('/ ', '/')
    if cur:
        lines.append(cur)
    return lines


def draw_uc(draw, alias, font, bold):
    cx, cy = UC_POS[alias]
    w, h = uc_size(alias)
    draw.ellipse((cx-w/2, cy-h/2, cx+w/2, cy+h/2), outline='black', width=3)

    idbb = draw.textbbox((0,0), alias, font=bold)
    lines = wrap_text(draw, EXPECTED_UCS[alias], font, w-34)
    total = 30 + len(lines)*29
    y = cy - total/2 + 3
    draw.text((cx-(idbb[2]-idbb[0])/2, y), alias, font=bold, fill='black')
    y += 31
    for line in lines:
        bb = draw.textbbox((0,0), line, font=font)
        draw.text((cx-(bb[2]-bb[0])/2, y), line, font=font, fill='black')
        y += 29


def actor_anchor(alias, target):
    x, y = ACTOR_POS[alias]
    tx, ty = target

    if alias == 'User':
        # Separate three authentication rays immediately.
        if tx < x:
            return (x-20, y+18)
        if tx > x:
            return (x+20, y+18)
        return (x, y+42)

    if alias in ('Time', 'Email'):
        return (x, y-60)

    if x < tx:
        return (x+24, y-6)
    return (x-24, y-6)


def ellipse_edge(alias, toward):
    cx, cy = UC_POS[alias]
    w, h = uc_size(alias)
    rx, ry = w/2.0, h/2.0
    dx, dy = toward[0]-cx, toward[1]-cy
    if dx == 0 and dy == 0:
        return (cx, cy)
    scale = 1.0 / math.sqrt((dx*dx)/(rx*rx) + (dy*dy)/(ry*ry))
    return (cx + dx*scale, cy + dy*scale)


def draw_straight(draw, actor, uc):
    target = UC_POS[uc]
    start = actor_anchor(actor, target)
    end = ellipse_edge(uc, start)
    draw.line((start[0], start[1], end[0], end[1]), fill='black', width=3)


def draw_ortho(draw, points):
    # Explicit segment-by-segment drawing preserves square 90-degree bends.
    for p1, p2 in zip(points, points[1:]):
        draw.line((p1[0], p1[1], p2[0], p2[1]), fill='black', width=3)


def right_edge(alias):
    cx, cy = UC_POS[alias]
    w, _ = uc_size(alias)
    return (cx+w/2, cy)


def draw_exception_routes(draw):
    # Admin -> UC21.
    # Direct line would cross Reception's straight rays. Route completely outside
    # the actor column, then enter horizontally through a clear row.
    ax, ay = ACTOR_POS['Admin']
    ex, ey = right_edge('UC21')
    draw_ortho(draw, [
        (ax+24, ay-6),
        (1690, ay-6),
        (1690, ey),
        (ex, ey),
    ])

    # Reception -> UC13.
    # Direct line would cut UC15 / the payment line. Use a separate outer channel
    # and re-enter at UC13's clear row. No shared trunk with Admin -> UC21.
    rx, ry = ACTOR_POS['Reception']
    ex, ey = right_edge('UC13')
    draw_ortho(draw, [
        (rx+24, ry-6),
        (1650, ry-6),
        (1650, ey),
        (ex, ey),
    ])


def main():
    verify_semantics()

    img = Image.new('RGB', (W,H), 'white')
    draw = ImageDraw.Draw(img)
    f = ImageFont.truetype(REGULAR, 29)
    fb = ImageFont.truetype(BOLD, 29)
    fa = ImageFont.truetype(REGULAR, 27)
    ft = ImageFont.truetype(BOLD, 31)

    # Draw connectors first so UC/actor outlines remain visually dominant.
    bent = {('Admin','UC21'), ('Reception','UC13')}
    for actor, uc in sorted(EXPECTED_ASSOC):
        if (actor, uc) in bent:
            continue
        draw_straight(draw, actor, uc)
    draw_exception_routes(draw)

    x1, y1, x2, y2 = BOUNDARY
    draw.rectangle((x1,y1,x2,y2), outline='black', width=3)
    draw.rectangle((x1+12,y1+6,x1+610,y1+58), fill='white')
    draw.text(
        (x1+24,y1+18),
        'HỆ THỐNG QUẢN LÝ TIÊM CHỦNG',
        font=ft,
        fill='black',
    )

    for alias in UC_POS:
        draw_uc(draw, alias, f, fb)
    for alias in ACTOR_POS:
        draw_actor(draw, alias, fa)

    img.save(PNG, dpi=(180,180))
    print(f'postprocessed {PNG.name}: {img.size}')
    print('verified: 21 UC / 10 visible actors / 27 direct associations')
    print('routing: 25 straight / 2 orthogonal / 0 visible actor generalizations')


if __name__ == '__main__':
    main()
