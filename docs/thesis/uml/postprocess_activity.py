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
    "Hinh_2_2_Activity_Nhap_Vac_Xin.png": dict(left_pad=0, right_pad=90, entities=[
        dict(label=": Chứng từ giao nhận", cx=895, cy=305, target=(780,305), mode="read"),
        dict(label=": Lô vắc xin", cx=895, cy=1115, target=(780,1115), mode="write"),
    ]),
    "Hinh_2_3_Activity_Quan_Ly_Ho_So.png": dict(left_pad=0, right_pad=220, entities=[
        dict(label=": Hồ sơ khách hàng", cx=1160, cy=465, target=(1030,465), mode="read"),
        dict(label=": Hồ sơ người được tiêm", cx=1380, cy=620, target=(1245,620), mode="write"),
    ]),
    "Hinh_2_4_Activity_Dang_Ky_Dich_Vu.png": dict(left_pad=0, right_pad=170, entities=[
        dict(label=": Hồ sơ người được tiêm", cx=930, cy=330, target=(780,330), mode="read"),
        dict(label=": Đăng ký dịch vụ", cx=1000, cy=880, target=(880,880), mode="write"),
    ]),
    "Hinh_2_5_Activity_Quan_Ly_Lich_Hen.png": dict(left_pad=0, right_pad=0, entities=[
        dict(label=": Đăng ký dịch vụ", cx=1250, cy=335, target=(1100,335), mode="read"),
        dict(label=": Lịch hẹn", cx=1260, cy=480, target=(1160,480), mode="write"),
    ]),
    "Hinh_2_6_Activity_Kham_Sang_Loc.png": dict(left_pad=0, right_pad=0, title="Bác sĩ", top_pad=34, entities=[
        dict(label=": Hồ sơ người được tiêm", cx=900, cy=310, target=(690,310), mode="read"),
        dict(label=": Kết quả khám sàng lọc", cx=900, cy=570, target=(700,570), mode="write"),
    ]),
    "Hinh_2_7_Activity_Thanh_Toan.png": dict(left_pad=260, right_pad=0, entities=[
        dict(label=": Đăng ký dịch vụ", cx=300, cy=235, target=(220,235), mode="read"),
        dict(label=": Giao dịch thanh toán", cx=130, cy=1100, target=(20,1100), mode="write"),
    ]),
    "Hinh_2_8_Activity_Tiem_Va_Theo_Doi.png": dict(left_pad=160, right_pad=0, entities=[
        dict(label=": Lô vắc xin", cx=80, cy=335, target=(20,335), mode="read"),
        dict(label=": Mũi tiêm đã thực hiện", cx=80, cy=760, target=(30,760), mode="write"),
        dict(label=": Theo dõi sau tiêm", cx=80, cy=1360, target=(40,1360), mode="write"),
    ]),
}

def dashed(draw, p1, p2, fill="black", width=2, dash=9, gap=6):
    x1,y1=p1; x2,y2=p2
    dx,dy=x2-x1,y2-y1
    length=math.hypot(dx,dy)
    if not length:
        return
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

def draw_entity(draw, cx, cy, label, font):
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
    return r

def connect_object_flow(draw, entity_center, target, mode, radius=27):
    cx,cy=entity_center
    tx,ty=target
    # Entity and target must be aligned horizontally; this is a hard visual rule.
    if abs(cy-ty) > 1:
        raise ValueError(f"ObjectFlow must be horizontal: entity={entity_center}, target={target}")
    if cx < tx:
        entity_edge=(cx+radius,cy)
        target_edge=(tx,ty)
    else:
        entity_edge=(cx-radius,cy)
        target_edge=(tx,ty)
    if mode=="read":
        dashed(draw,entity_edge,target_edge)
        arrowhead(draw,entity_edge,target_edge)
    elif mode=="write":
        dashed(draw,target_edge,entity_edge)
        arrowhead(draw,target_edge,entity_edge)
    else:
        raise ValueError(f"Unknown ObjectFlow mode: {mode}")

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
            raise SystemExit(f"{name}: base size changed from {expected} to {img.size}; review ObjectFlow coordinates before render")

        ow,oh=img.size
        top_pad=cfg.get("top_pad",0)
        if top_pad:
            padded=Image.new("RGB",(ow,oh+top_pad),"white")
            padded.paste(img,(0,top_pad))
            img=padded
            oh += top_pad

        left_pad=cfg.get("left_pad",0)
        right_pad=cfg.get("right_pad",0)
        nw=left_pad+ow+right_pad
        canvas=Image.new("RGB",(nw,oh),"white")
        canvas.paste(img,(left_pad,0))
        draw=ImageDraw.Draw(canvas)

        if cfg.get("title"):
            title=cfg["title"]
            box=draw.textbbox((0,0),title,font=title_font)
            center=left_pad+ow/2
            draw.text((center-(box[2]-box[0])/2,7),title,font=title_font,fill="black")

        for entity in cfg["entities"]:
            cx=entity["cx"]
            cy=entity["cy"]+top_pad
            tx,ty=entity["target"]
            tx += left_pad
            ty += top_pad

            # cx is final-canvas coordinate by design. For diagrams with left padding,
            # entities live in that padding while activity coordinates shift with left_pad.
            radius=draw_entity(draw,cx,cy,entity["label"],font)
            connect_object_flow(draw,(cx,cy),(tx,ty),entity["mode"],radius=radius)

        draw.rectangle((1,1,nw-2,oh-2),outline="black",width=2)
        canvas.save(path,dpi=(180,180))
        print(f"postprocessed {name}: {expected} -> {canvas.size}")

if __name__=="__main__":
    main()
