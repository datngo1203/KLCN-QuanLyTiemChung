from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math, re

ROOT = Path(__file__).resolve().parent
PUML = ROOT / 'Hinh_System_Use_Case_21UC_Full_Portrait.puml'
PNG = ROOT / 'Hinh_System_Use_Case_21UC_Full_Portrait.png'
W,H = 1900,3000
BOUNDARY=(250,280,1380,2670)

EXPECTED_ACTORS={
 'User':'Người dùng','Employee':'Nhân viên','Customer':'Khách hàng','Admin':'Quản trị viên',
 'Reception':'Lễ tân','Doctor':'Bác sĩ','Cashier':'Thu ngân','Nurse':'Điều dưỡng',
 'Warehouse':'Nhân viên kho','Time':'Thời gian','Email':'Dịch vụ thư điện tử'}
EXPECTED_UCS={
 'UC01':'Đăng ký tài khoản khách hàng','UC02':'Đăng nhập hệ thống','UC03':'Đổi mật khẩu','UC04':'Đăng xuất hệ thống',
 'UC05':'Thay đổi chức danh tài khoản nhân viên','UC06':'Quản lý danh mục tiêm chủng','UC07':'Quản lý ưu đãi',
 'UC08':'Quản lý lô và tồn kho vắc xin','UC09':'Quản lý hồ sơ khách hàng/người được tiêm','UC10':'Tra cứu dịch vụ tiêm chủng',
 'UC11':'Đăng ký dịch vụ tiêm chủng','UC12':'Quản lý lịch hẹn','UC13':'Gửi nhắc lịch','UC14':'Tự động gửi nhắc lịch',
 'UC15':'Ghi nhận khám sàng lọc và chỉ định','UC16':'Thanh toán dịch vụ','UC17':'Ghi nhận mũi tiêm',
 'UC18':'Ghi nhận theo dõi sau tiêm','UC19':'Theo dõi hành trình tiêm chủng','UC20':'Tra cứu lịch sử tiêm chủng','UC21':'Báo cáo hoạt động'}
EXPECTED_ASSOC={
 ('User','UC02'),('User','UC03'),('User','UC04'),
 ('Customer','UC01'),('Customer','UC09'),('Customer','UC10'),('Customer','UC11'),('Customer','UC12'),('Customer','UC16'),('Customer','UC19'),('Customer','UC20'),
 ('Reception','UC09'),('Reception','UC11'),('Reception','UC12'),('Reception','UC13'),
 ('Doctor','UC15'),('Cashier','UC16'),('Nurse','UC17'),('Nurse','UC18'),('Warehouse','UC08'),
 ('Admin','UC05'),('Admin','UC06'),('Admin','UC07'),('Admin','UC21'),
 ('Time','UC14'),('Email','UC13'),('Email','UC14')}
EXPECTED_GENS={('Customer','User'),('Employee','User'),('Reception','Employee'),('Doctor','Employee'),('Cashier','Employee'),('Nurse','Employee'),('Warehouse','Employee'),('Admin','Employee')}

UC_POS={
 'UC02':(500,470),'UC03':(820,470),'UC04':(1140,470),
 'UC01':(500,720),'UC09':(820,870),'UC05':(1140,720),
 'UC10':(500,1020),'UC11':(820,1170),'UC06':(1140,1020),
 'UC19':(500,1320),'UC12':(820,1470),'UC07':(1140,1320),
 'UC20':(500,1620),'UC16':(820,1770),'UC21':(1140,1620),
 'UC13':(760,2070),'UC15':(1140,1970),
 'UC14':(760,2350),'UC17':(1140,2220),
 'UC18':(1140,2440),'UC08':(1140,2580),
}
ACTOR_POS={
 'User':(900,110),'Employee':(1780,1500),'Customer':(105,1320),
 'Admin':(1550,1050),'Reception':(1550,1450),'Cashier':(1550,1780),'Doctor':(1550,2020),
 'Nurse':(1550,2320),'Warehouse':(1550,2580),'Time':(600,2860),'Email':(1060,2860)}

REGULAR='/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman.ttf'
BOLD='/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman_Bold.ttf'
if not Path(REGULAR).exists(): REGULAR='/usr/share/fonts/truetype/liberation2/LiberationSerif-Regular.ttf'
if not Path(BOLD).exists(): BOLD='/usr/share/fonts/truetype/liberation2/LiberationSerif-Bold.ttf'

def parse_source(text):
    actors={alias:name for name,alias in re.findall(r'^actor\s+"([^"]+)"\s+as\s+(\w+)\s*$',text,re.M)}
    ucs={}
    for label,alias in re.findall(r'^\s*usecase\s+"((?:[^"\\]|\\.)*)"\s+as\s+(UC\d+)\s*$',text,re.M):
        plain=label.replace('\\n',' ')
        plain=re.sub(r'^UC\d+\s+','',plain).strip()
        ucs[alias]=plain.replace('/ ','/').replace('  ',' ')
    gens=set()
    for a,b in re.findall(r'^\s*(\w+)\s+-[^\n]*\|>\s+(\w+)\s*$',text,re.M):
        if a in actors and b in actors: gens.add((a,b))
    assoc=set()
    parts=re.split(r"^'\s*-+\s*27 direct actor–UC associations\s*-+\s*$", text, maxsplit=1, flags=re.M)
    if len(parts) != 2:
        raise SystemExit('Cannot find 27 direct association section in PUML')
    for line in parts[1].splitlines():
        line=line.strip()
        if not line or line.startswith("'") or line.startswith('@'): continue
        m=re.match(r'^(\w+)\s+(?:--|-right-|-left-|-up-|-down-)\s+(\w+)\s*$',line)
        if not m: continue
        a,b=m.groups()
        if a in actors and b in ucs: assoc.add((a,b))
        elif b in actors and a in ucs: assoc.add((b,a))
    return actors,ucs,assoc,gens

def verify():
    actors,ucs,assoc,gens=parse_source(PUML.read_text(encoding='utf-8'))
    if actors != EXPECTED_ACTORS: raise SystemExit(f'actor semantic mismatch: {actors}')
    if ucs != EXPECTED_UCS: raise SystemExit(f'UC semantic mismatch: {ucs}')
    if assoc != EXPECTED_ASSOC: raise SystemExit(f'association mismatch missing={EXPECTED_ASSOC-assoc} extra={assoc-EXPECTED_ASSOC}')
    if gens != EXPECTED_GENS: raise SystemExit(f'generalization mismatch missing={EXPECTED_GENS-gens} extra={gens-EXPECTED_GENS}')

def uc_size(alias):
    s=EXPECTED_UCS[alias]
    if len(s)>=38: return (330,118)
    if len(s)>=29: return (300,108)
    if len(s)>=21: return (270,100)
    return (235,92)

def draw_actor(draw,alias,font):
    x,y=ACTOR_POS[alias]
    r=14; hy=y-46
    draw.ellipse((x-r,hy-r,x+r,hy+r),outline='black',width=3)
    draw.line((x,hy+r,x,y+12),fill='black',width=3)
    draw.line((x-24,y-6,x+24,y-6),fill='black',width=3)
    draw.line((x,y+12,x-22,y+42),fill='black',width=3)
    draw.line((x,y+12,x+22,y+42),fill='black',width=3)
    label=EXPECTED_ACTORS[alias]
    lines=[label]
    if label=='Dịch vụ thư điện tử': lines=['Dịch vụ thư','điện tử']
    yy=y+58
    for line in lines:
        bb=draw.textbbox((0,0),line,font=font)
        draw.text((x-(bb[2]-bb[0])/2,yy),line,font=font,fill='black')
        yy+=32

def wrap_text(draw,text,font,maxw):
    words=text.replace('/','/ ').split()
    lines=[]; cur=''
    for w in words:
        t=(cur+' '+w).strip().replace('/ ','/')
        bb=draw.textbbox((0,0),t,font=font)
        if bb[2]-bb[0] <= maxw or not cur:
            cur=t
        else:
            lines.append(cur); cur=w.replace('/ ','/')
    if cur: lines.append(cur)
    return lines

def draw_uc(draw,alias,font,bold):
    cx,cy=UC_POS[alias]; w,h=uc_size(alias)
    draw.ellipse((cx-w/2,cy-h/2,cx+w/2,cy+h/2),outline='black',width=3)
    idbb=draw.textbbox((0,0),alias,font=bold)
    lines=wrap_text(draw,EXPECTED_UCS[alias],font,w-34)
    total=30+len(lines)*29
    y=cy-total/2+3
    draw.text((cx-(idbb[2]-idbb[0])/2,y),alias,font=bold,fill='black'); y+=31
    for line in lines:
        bb=draw.textbbox((0,0),line,font=font)
        draw.text((cx-(bb[2]-bb[0])/2,y),line,font=font,fill='black'); y+=29

def edge(alias,side):
    cx,cy=UC_POS[alias]; w,h=uc_size(alias)
    if side=='left': return (cx-w/2,cy)
    if side=='right': return (cx+w/2,cy)
    if side=='top': return (cx,cy-h/2)
    return (cx,cy+h/2)

def poly(draw,pts,width=3):
    draw.line(pts,fill='black',width=width,joint='curve')

def triangle(draw,tip,frompt,size=15):
    tx,ty=tip; fx,fy=frompt
    dx,dy=tx-fx,ty-fy; L=math.hypot(dx,dy) or 1
    ux,uy=dx/L,dy/L; px,py=-uy,ux
    base=(tx-ux*size*1.45,ty-uy*size*1.45)
    p1=(base[0]+px*size*.75,base[1]+py*size*.75)
    p2=(base[0]-px*size*.75,base[1]-py*size*.75)
    draw.polygon([tip,p1,p2],fill='white',outline='black')
    draw.line([tip,p1,p2,tip],fill='black',width=2)

def generalization(draw,child,parent):
    cx,cy=ACTOR_POS[child]
    if child=='Customer' and parent=='User':
        pts=[(cx,cy-66),(cx,110),(840,110)]
        tip=(840,110)
    elif child=='Employee' and parent=='User':
        pts=[(cx+28,cy-55),(1850,cy-55),(1850,110),(958,110)]
        tip=(958,110)
    else:
        order=['Admin','Reception','Cashier','Doctor','Nurse','Warehouse']
        idx=order.index(child)
        start=(cx+24,cy-34)
        tip=(1738,1455+idx*18)
        pts=[start,tip]
    poly(draw,pts,3)
    triangle(draw,tip,pts[-2],16)

def customer_routes(draw):
    targets=sorted(['UC01','UC09','UC10','UC11','UC19','UC12','UC20','UC16'],key=lambda a:UC_POS[a][1])
    channels=[175,185,195,205,215,225,235,245]
    anchors=[(130,1245+i*21) for i in range(8)]
    for alias,ch,st in zip(targets,channels,anchors):
        ex,ey=edge(alias,'left')
        poly(draw,[st,(ch,st[1]),(ch,ey),(ex,ey)])

def right_fan(draw,actor,targets,base_channel):
    targets=sorted(targets,key=lambda a:UC_POS[a][1])
    ax,ay=ACTOR_POS[actor]; n=len(targets)
    starts=[(ax-30, ay-(n-1)*18 + i*36) for i in range(n)]
    for i,(alias,st) in enumerate(zip(targets,starts)):
        ex,ey=edge(alias,'right'); ch=base_channel+i*18
        poly(draw,[st,(ch,st[1]),(ch,ey),(ex,ey)])

def user_routes(draw):
    ux,uy=ACTOR_POS['User']
    starts=[(ux-24,uy+45),(ux,uy+48),(ux+24,uy+45)]
    for st,alias in zip(starts,['UC02','UC03','UC04']):
        ex,ey=edge(alias,'top')
        poly(draw,[st,(ex,ey)])

def single_right(draw,actor,alias):
    ax,ay=ACTOR_POS[actor]; ex,ey=edge(alias,'right')
    poly(draw,[(ax-30,ay-5),(1450,ay-5),(1450,ey),(ex,ey)])

def reminder_routes(draw):
    ex,ey=edge('UC14','bottom'); ax,ay=ACTOR_POS['Time']
    poly(draw,[(ax,ay-65),(ax,2700),(700,2700),(700,ey),(ex,ey)])
    ax,ay=ACTOR_POS['Email']
    ex,ey=edge('UC13','bottom')
    poly(draw,[(ax-18,ay-65),(980,ay-65),(980,2185),(ex+60,2185),(ex+60,ey),(ex,ey)])
    ex2,ey2=edge('UC14','bottom')
    poly(draw,[(ax+18,ay-65),(1030,ay-65),(1030,2490),(ex2+70,2490),(ex2+70,ey2),(ex2,ey2)])

def main():
    verify()
    img=Image.new('RGB',(W,H),'white'); d=ImageDraw.Draw(img)
    f=ImageFont.truetype(REGULAR,29); fb=ImageFont.truetype(BOLD,29)
    fa=ImageFont.truetype(REGULAR,27); ft=ImageFont.truetype(BOLD,31)
    x1,y1,x2,y2=BOUNDARY

    user_routes(d)
    customer_routes(d)
    right_fan(d,'Reception',['UC09','UC11','UC12','UC13'],1410)
    right_fan(d,'Admin',['UC05','UC06','UC07','UC21'],1410)
    single_right(d,'Cashier','UC16')
    single_right(d,'Doctor','UC15')
    right_fan(d,'Nurse',['UC17','UC18'],1410)
    single_right(d,'Warehouse','UC08')
    reminder_routes(d)

    for child,parent in [('Customer','User'),('Employee','User'),('Admin','Employee'),('Reception','Employee'),('Cashier','Employee'),('Doctor','Employee'),('Nurse','Employee'),('Warehouse','Employee')]:
        generalization(d,child,parent)

    d.rectangle((x1,y1,x2,y2),outline='black',width=3)
    d.rectangle((x1+12,y1+6,x1+610,y1+58),fill='white')
    d.text((x1+24,y1+18),'HỆ THỐNG QUẢN LÝ TIÊM CHỦNG',font=ft,fill='black')
    for alias in UC_POS: draw_uc(d,alias,f,fb)
    for alias in ACTOR_POS: draw_actor(d,alias,fa)

    img.save(PNG,dpi=(180,180))
    print(f'postprocessed {PNG.name}: {img.size}')
    print('verified 21 UC / 11 actors / 27 associations / 8 generalizations')

if __name__=='__main__':
    main()
