# Windows/WPF rules — KLCN090

Áp dụng cho `apps/windows/**`. Root `AGENTS.md`, `SCOPE.md` và active issue vẫn là source-of-truth.

## Structure
- WPF là employee client; gọi API thật, không truy cập SQL trực tiếp.
- Ưu tiên `ViewsV2/`, `Themes/` và component/token `CE.*` hiện có; không redesign toàn shell khi task chỉ chạm một module.
- Windows có thể khác Web để tối ưu nghiệp vụ nhân viên, nhưng giữ consistency màu/chữ/status đã chốt.
- Với UI task đáng kể, kiểm skill UI theo SKILL GATE; prototype phải được chuyển thành XAML/component thật và không commit artifact nghiên cứu không cần thiết.

## UX
- Thiết kế theo actor/task, không chỉ xếp toàn bộ field và button lên một màn.
- Ưu tiên tra cứu/master-detail, keyboard, readability và thao tác liên tục.
- Confirmation native cho destructive/high-impact mutation khi acceptance yêu cầu.
- Không lộ raw enum/object/provider error. Nút disabled phải có state/reason hợp lý.
- Unknown POST outcome: hiển thị trạng thái chưa xác định và cho reconcile/reload; không blind replay.
- Không coi state UI là state SQL.

## Gates
- Windows tests + WPF render/control tests liên quan + Release build.
- UI/flow thay đổi cần full-shell smoke: kích thước cửa sổ phù hợp, keyboard/Tab/Enter, normal/empty/error/permission và persistence/reload.
- Nếu runtime không chạy được thì ghi `UNVERIFIED`; CI/build không thay full-shell.
