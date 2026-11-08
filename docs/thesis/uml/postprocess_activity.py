from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageOps
import cv2
import math
import numpy as np
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

# hint is strictly inside the target Activity/Decision. It identifies the node
# only; ObjectFlow endpoints are computed from the node's rendered contour.
CFG = {
    "Hinh_2_2_Activity_Nhap_Vac_Xin.png": dict(entities=[
        dict(label=": Chứng từ giao nhận", hint=(632,333), side="right", mode="read"),
        dict(label=": Lô vắc xin", hint=(632,1162), side="right", mode="write"),
    ]),
    "Hinh_2_3_Activity_Quan_Ly_Ho_So.png": dict(entities=[
        dict(label=": Hồ sơ khách hàng", hint=(912,460), side="right", mode="read"),
        dict(label=": Hồ sơ người được tiêm", hint=(1104,634), side="right", mode="write"),
    ]),
    "Hinh_2_4_Activity_Dang_Ky_Dich_Vu.png": dict(entities=[
        dict(label=": Hồ sơ người được tiêm", hint=(644,333), side="right", mode="read"),
        dict(label=": Đăng ký dịch vụ", hint=(644,936), side="right", mode="write"),
    ]),
    "Hinh_2_5_Activity_Quan_Ly_Lich_Hen.png": dict(entities=[
        dict(label=": Đăng ký dịch vụ", hint=(920,333), side="right", mode="read"),
        dict(label=": Lịch hẹn", hint=(920,460), side="right", mode="write"),
    ]),
    "Hinh_2_6_Activity_Kham_Sang_Loc.png": dict(title="Bác sĩ", top_pad=34, entities=[
        dict(label=": Hồ sơ người được tiêm", hint=(569,287), side="right", mode="read"),
        dict(label=": Kết quả khám sàng lọc", hint=(569,577), side="right", mode="write"),
    ]),
    "Hinh_2_7_Activity_Thanh_Toan.png": dict(entities=[
        dict(label=": Đăng ký dịch vụ", hint=(402,188), side="left", mode="read"),
        dict(label=": Giao dịch thanh toán", hint=(209,1140), side="left", mode="write"),
    ]),
    "Hinh_2_8_Activity_Tiem_Va_Theo_Doi.png": dict(entities=[
        dict(label=": Lô vắc xin", hint=(248,333), side="left", mode="read"),
        dict(label=": Mũi tiêm đã thực hiện", hint=(248,809), side="left", mode="write"),
        dict(label=": Theo dõi sau tiêm", hint=(248,1395), side="left", mode="write"),
    ]),
}

BLACK_THRESHOLD = 200
ENTITY_RADIUS = 27
ENTITY_GAP = 22
OUTER_MARGIN = 14


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


def entity_text_lines(label):
    return textwrap.wrap(label,width=22) or [label]


def entity_bbox(cx, cy, label, font):
    r=ENTITY_RADIUS
    lines=entity_text_lines(label)
    max_w=0
    for line in lines:
        box=font.getbbox(line)
        max_w=max(max_w,box[2]-box[0])
    x1=min(cx-r-10, cx-max_w/2)
    x2=max(cx+r+10, cx+max_w/2)
    y1=cy-r
    y2=cy+r+12+22*len(lines)
    return (int(math.floor(x1)),int(math.floor(y1)),
            int(math.ceil(x2)),int(math.ceil(y2)))


def draw_entity(draw, cx, cy, label, font):
    r=ENTITY_RADIUS
    draw.ellipse((cx-r,cy-r,cx+r,cy+r),outline="black",width=2)
    draw.line((cx+4,cy+19,cx+21,cy+2),fill="black",width=2)
    draw.line((cx-r-10,cy+r+5,cx+r+10,cy+r+5),fill="black",width=2)
    y=cy+r+12
    for line in entity_text_lines(label):
        box=draw.textbbox((0,0),line,font=font)
        draw.text((cx-(box[2]-box[0])/2,y),line,font=font,fill="black")
        y += 22


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


def find_closed_shape_bbox(img, hint):
    gray=np.array(img.convert("L"))
    _,mask=cv2.threshold(gray,BLACK_THRESHOLD,255,cv2.THRESH_BINARY_INV)
    contours,_=cv2.findContours(mask,cv2.RETR_LIST,cv2.CHAIN_APPROX_SIMPLE)
    hx,hy=hint
    candidates=[]
    for contour in contours:
        x,y,w,h=cv2.boundingRect(contour)
        area=cv2.contourArea(contour)
        if not (x < hx < x+w-1 and y < hy < y+h-1):
            continue
        if not (80 <= w <= 700 and 35 <= h <= 170):
            continue
        if area < 3000:
            continue
        fill_ratio=area/max(1,w*h)
        if fill_ratio < 0.18:
            continue
        candidates.append((w*h,x,y,w,h,area,fill_ratio))
    if not candidates:
        raise RuntimeError(f"Cannot locate target UML shape for hint={hint}")
    candidates.sort(key=lambda item:item[0])
    if len(candidates)>1 and candidates[1][0] < candidates[0][0]*1.08:
        raise RuntimeError(
            f"Ambiguous target UML shape for hint={hint}: "
            f"{candidates[0][1:5]} vs {candidates[1][1:5]}"
        )
    _,x,y,w,h,_,_=candidates[0]
    return (x,y,w,h)


def route_dark_pixels(img, p1, p2):
    x1,y1=p1; x2,y2=p2
    if y1 != y2:
        raise ValueError("ObjectFlow route must be horizontal")
    lo,hi=sorted((x1,x2))
    lo+=4; hi-=4
    if hi <= lo:
        return 0
    gray=np.array(img.convert("L"))
    h,w=gray.shape
    y=max(0,min(h-1,y1))
    xa=max(0,lo); xb=min(w-1,hi)
    if xb <= xa:
        return 0
    band=gray[max(0,y-2):min(h,y+3),xa:xb+1]
    return int(np.count_nonzero(band < BLACK_THRESHOLD))


def choose_entity_geometry(img, shape_bbox, label, side, font):
    x,y,w,h=shape_bbox
    cy=y+h//2
    if side=="right":
        target_edge=(x+w-1,cy)
        cx=target_edge[0]+ENTITY_GAP+ENTITY_RADIUS
        entity_edge=(cx-ENTITY_RADIUS,cy)
        step=10
    elif side=="left":
        target_edge=(x,cy)
        cx=target_edge[0]-ENTITY_GAP-ENTITY_RADIUS
        entity_edge=(cx+ENTITY_RADIUS,cy)
        step=-10
    else:
        raise ValueError(f"Unknown entity side: {side}")

    gray=np.array(img.convert("L"))
    for _ in range(40):
        dark=route_dark_pixels(img,target_edge,entity_edge)
        bbox=entity_bbox(cx,cy,label,font)
        ix1=max(0,bbox[0]); iy1=max(0,bbox[1])
        ix2=min(img.width-1,bbox[2]); iy2=min(img.height-1,bbox[3])
        overlap_dark=0
        if ix2>ix1 and iy2>iy1:
            region=gray[iy1:iy2+1,ix1:ix2+1]
            overlap_dark=int(np.count_nonzero(region < BLACK_THRESHOLD))
        if dark <= 8 and overlap_dark <= 20:
            return dict(cx=cx,cy=cy,target_edge=target_edge,
                        entity_edge=entity_edge,bbox=bbox)
        cx+=step
        entity_edge=(cx-ENTITY_RADIUS,cy) if side=="right" else (cx+ENTITY_RADIUS,cy)

    raise RuntimeError(
        f"Cannot place entity '{label}' on {side} without crossing rendered content"
    )


def connect_object_flow(draw, target_edge, entity_edge, mode):
    if target_edge[1] != entity_edge[1]:
        raise ValueError("Shape-aware ObjectFlow must be horizontal")
    if mode=="read":
        dashed(draw,entity_edge,target_edge)
        arrowhead(draw,entity_edge,target_edge)
    elif mode=="write":
        dashed(draw,target_edge,entity_edge)
        arrowhead(draw,target_edge,entity_edge)
    else:
        raise ValueError(f"Unknown ObjectFlow mode: {mode}")


def main():
    font=ImageFont.truetype(FONT_PATH,18)
    title_font=ImageFont.truetype(FONT_PATH,22)

    for name,cfg in CFG.items():
        path=ROOT/name
        base=normalize_base(path)
        expected=EXPECTED_BASE_SIZES[name]
        if base.size != expected:
            raise SystemExit(
                f"{name}: base size changed from {expected} to {base.size}; "
                "review shape hints before render"
            )

        placements=[]
        for entity in cfg["entities"]:
            shape_bbox=find_closed_shape_bbox(base,entity["hint"])
            geom=choose_entity_geometry(
                base,shape_bbox,entity["label"],entity["side"],font
            )
            placements.append((entity,shape_bbox,geom))
            print(
                f"{name}: {entity['label']} hint={entity['hint']} "
                f"shape={shape_bbox} target_edge={geom['target_edge']} "
                f"entity_edge={geom['entity_edge']}"
            )

        min_x=0; max_x=base.width
        min_y=0; max_y=base.height
        for entity,shape_bbox,geom in placements:
            x1,y1,x2,y2=geom["bbox"]
            min_x=min(min_x,x1)
            max_x=max(max_x,x2)
            min_y=min(min_y,y1)
            max_y=max(max_y,y2)

        top_pad=cfg.get("top_pad",0)
        left_pad=max(0,OUTER_MARGIN-min_x)
        right_pad=max(0,max_x+OUTER_MARGIN-base.width)
        bottom_pad=max(0,max_y+OUTER_MARGIN-base.height)

        nw=left_pad+base.width+right_pad
        nh=top_pad+base.height+bottom_pad
        canvas=Image.new("RGB",(nw,nh),"white")
        canvas.paste(base,(left_pad,top_pad))
        draw=ImageDraw.Draw(canvas)

        if cfg.get("title"):
            title=cfg["title"]
            box=draw.textbbox((0,0),title,font=title_font)
            center=left_pad+base.width/2
            draw.text(
                (center-(box[2]-box[0])/2,7),
                title,font=title_font,fill="black"
            )

        for entity,shape_bbox,geom in placements:
            cx=geom["cx"]+left_pad
            cy=geom["cy"]+top_pad
            target_edge=(
                geom["target_edge"][0]+left_pad,
                geom["target_edge"][1]+top_pad,
            )
            entity_edge=(
                geom["entity_edge"][0]+left_pad,
                geom["entity_edge"][1]+top_pad,
            )
            draw_entity(draw,cx,cy,entity["label"],font)
            connect_object_flow(
                draw,target_edge,entity_edge,entity["mode"]
            )

        draw.rectangle((1,1,nw-2,nh-2),outline="black",width=2)
        canvas.save(path,dpi=(180,180))
        print(f"postprocessed {name}: {expected} -> {canvas.size}")


if __name__=="__main__":
    main()
