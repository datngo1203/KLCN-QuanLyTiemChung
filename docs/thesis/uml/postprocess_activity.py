from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageOps
import math
import textwrap

ROOT = Path(__file__).resolve().parent
FONT_PATH = "/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf"

EXPECTED_BASE_SIZES = {
    "Hinh_2_2_Activity_Nhap_Vac_Xin.png": (919, 1420),
    "Hinh_2_3_Activity_Quan_Ly_Ho_So.png": (1292, 1451),
    "Hinh_2_4_Activity_Dang_Ky_Dich_Vu.png": (964, 1731),
    "Hinh_2_5_Activity_Quan_Ly_Lich_Hen.png": (1383, 1447),
    "Hinh_2_6_Activity_Kham_Sang_Loc.png": (1352, 1026),
    "Hinh_2_7_Activity_Thanh_Toan.png": (1348, 1299),
    "Hinh_2_8_Activity_Tiem_Va_Theo_Doi.png": (1066, 1543),
}

CFG = {
    "Hinh_2_2_Activity_Nhap_Vac_Xin.png": dict(side="right", panel=300, entities=[
        dict(label=": Chứng từ giao nhận", y=290, target=(615,305), mode="read"),
        dict(label=": Lô vắc xin", y=1080, target=(610,1115), mode="write"),
        dict(label=": Tồn kho", y=1240, target=(610,1115), mode="write"),
    ]),
    "Hinh_2_3_Activity_Quan_Ly_Ho_So.png": dict(side="right", panel=260, entities=[
        dict(label=": Hồ sơ khách hàng", y=465, target=(930,465), mode="read"),
        dict(label=": Hồ sơ người được tiêm", y=660, target=(1080,620), mode="write"),
    ]),
    "Hinh_2_4_Activity_Dang_Ky_Dich_Vu.png": dict(side="right", panel=320, entities=[
        dict(label=": Hồ sơ người được tiêm", y=330, target=(650,330), mode="read"),
        dict(label=": Đăng ký dịch vụ", y=875, target=(625,870), mode="write"),
    ]),
    "Hinh_2_5_Activity_Quan_Ly_Lich_Hen.png": dict(side="right", panel=260, entities=[
        dict(label=": Đăng ký dịch vụ", y=335, target=(1030,350), mode="read"),
        dict(label=": Lịch hẹn", y=520, target=(1015,495), mode="write"),
    ]),
    "Hinh_2_6_Activity_Kham_Sang_Loc.png": dict(side="right", panel=260, title="Bác sĩ", top_pad=34, entities=[
        dict(label=": Hồ sơ người được tiêm", y=260, target=(650,260), mode="read"),
        dict(label=": Kết quả khám / chỉ định", y=575, target=(650,515), mode="write"),
    ]),
    "Hinh_2_7_Activity_Thanh_Toan.png": dict(side="left", panel=260, entities=[
        dict(label=": Đăng ký dịch vụ", y=240, target=(430,235), mode="read"),
        dict(label=": Giao dịch thanh toán", y=1090, target=(420,1100), mode="write"),
    ]),
    "Hinh_2_8_Activity_Tiem_Va_Theo_Doi.png": dict(side="left", panel=320, entities=[
        dict(label=": Lô vắc xin", y=340, target=(335,335), mode="read"),
        dict(label=": Mũi tiêm đã thực hiện", y=765, target=(330,760), mode="write"),
        dict(label=": Theo dõi sau tiêm", y=1360, target=(340,1360), mode="write"),
    ]),
}

def dashed(draw, pts, fill="black", width=2, dash=9, gap=6):
    for (x1,y1),(x2,y2) in zip(pts,pts[1:]):
        dx,dy=x2-x1,y2-y1
        length=math.hypot(dx,dy)
        if not length:
            continue
        ux,uy=dx/length,dy/length
        d=0
        while d<length:
            e=min(d+dash,length)
            draw.line((x1+ux*d,y1+uy*d,x1+ux*e,y1+uy*e),fill=fill,width=width)
            d += dash+gap

def arrowhead(draw, p_from, p_to, size=10, fill="black"):
    x1,y1=p_from; x2,y2=p_to
    angle=math.atan2(y2-y1,x2-x1)
    a1=angle+math.pi*0.82
    a2=angle-math.pi*0.82
    draw.polygon([
        (x2,y2),
        (x2+size*math.cos(a1),y2+size*math.sin(a1)),
        (x2+size*math.cos(a2),y2+size*math.sin(a2)),
    ],fill=fill)

def draw_entity(draw, cx, cy, label, side, font):
    r=27
    draw.ellipse((cx-r,cy-r,cx+r,cy+r),outline="black",width=2)
    draw.line((cx+4,cy+19,cx+21,cy+2),fill="black",width=2)
    draw.line((cx-r-10,cy+r+5,cx+r+10,cy+r+5),fill="black",width=2)
    lines=textwrap.wrap(label,width=22) or [label]
    y=cy+r+12
    for line in lines:
        box=draw.textbbox((0,0),line,font=font)
        draw.text((cx-(box[2]-box[0])/2,y),line,font=font,fill="black")
        y += 22
    return (cx-r-12,cy) if side=="right" else (cx+r+12,cy)

def normalize_base(path):
    img=Image.open(path).convert("RGB")
    white=Image.new("RGB",img.size,"white")
    bbox=ImageChops.difference(img,white).getbbox()
    if bbox:
        img=img.crop(bbox)
    draw=ImageDraw.Draw(img)
    w,h=img.size
    edge_strip=4
    draw.rectangle((0,0,edge_strip,h-1),fill="white")
    draw.rectangle((w-1-edge_strip,0,w-1,h-1),fill="white")
    img=ImageOps.expand(img,border=14,fill="white")
    return img

def main():
    font=ImageFont.truetype(FONT_PATH,18)
    title_font=ImageFont.truetype(FONT_PATH,22)
    for name,cfg in CFG.items():
        path=ROOT/name
        img=normalize_base(path)
        expected=EXPECTED_BASE_SIZES[name]
        if img.size != expected:
            raise SystemExit(f"{name}: base size changed from {expected} to {img.size}; review data-flow coordinates before render")

        ow,oh=img.size
        top_pad=cfg.get("top_pad",0)
        if top_pad:
            padded=Image.new("RGB",(ow,oh+top_pad),"white")
            padded.paste(img,(0,top_pad))
            img=padded
            oh += top_pad

        panel=cfg["panel"]
        side=cfg["side"]
        nw=ow+panel
        canvas=Image.new("RGB",(nw,oh),"white")
        shift=panel if side=="left" else 0
        canvas.paste(img,(shift,0))
        draw=ImageDraw.Draw(canvas)

        if cfg.get("title"):
            title=cfg["title"]
            box=draw.textbbox((0,0),title,font=title_font)
            center=shift+ow/2
            draw.text((center-(box[2]-box[0])/2,7),title,font=title_font,fill="black")

        if side=="right":
            gutter=shift+ow+25
            cx=shift+ow+panel//2+10
        else:
            gutter=panel-25
            cx=panel//2-10

        for entity in cfg["entities"]:
            cy=entity["y"]+top_pad
            anchor=draw_entity(draw,cx,cy,entity["label"],side,font)
            tx,ty=entity["target"]
            tx += shift
            ty += top_pad
            path_points=[anchor,(gutter,cy),(gutter,ty),(tx,ty)]
            if entity["mode"]=="read":
                dashed(draw,path_points)
                arrowhead(draw,path_points[-2],path_points[-1])
            else:
                rev=list(reversed(path_points))
                dashed(draw,rev)
                arrowhead(draw,rev[-2],rev[-1])

        draw.rectangle((1,1,nw-2,oh-2),outline="black",width=2)
        canvas.save(path,dpi=(180,180))
        print(f"postprocessed {name}: {expected} -> {canvas.size}")

if __name__=="__main__":
    main()
