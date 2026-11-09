from pathlib import Path
import re

ROOT = Path(__file__).resolve().parent
PUML = ROOT / "Hinh_2_9_System_Use_Case_Tong_Quan.puml"
PNG = ROOT / "Hinh_2_9_System_Use_Case_Tong_Quan.png"

# Validator only: canonical Hình 2.9 PNG is the raw PlantUML render.

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

FORBIDDEN_UCS = {"UC02", "UC03", "UC04"}


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


def main():
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
    if FORBIDDEN_UCS & set(ucs):
        raise SystemExit(f"authentication UCs leaked into overview: {FORBIDDEN_UCS & set(ucs)}")
    if re.search(r'<<\s*(include|extend)\s*>>', text, re.I):
        raise SystemExit("include/extend must not appear in Hình 2.9 overview")
    if "|>" in text:
        raise SystemExit("generalization must not appear in Hình 2.9 overview")
    if not PNG.exists():
        raise SystemExit("PlantUML PNG was not rendered")

    print("validated Hình 2.9: raw PlantUML canonical render")
    print("18 visible UC / 9 visible actors / 24 direct associations")
    print("0 include / 0 extend / 0 generalization; UC02/UC03/UC04 omitted from overview")


if __name__ == "__main__":
    main()
