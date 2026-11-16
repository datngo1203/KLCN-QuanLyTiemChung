# KLCN — Demo bảo vệ 17/11/2026 (tài liệu vận hành, branch riêng)

**Branch:** `docs/demo-defense-2026-11-17`. Tài liệu bàn giao ngoài luồng feature; **không merge vào `main`**, không sửa `docs/thesis-r1-current-state` hoặc branch DATA-R2. Source of truth CODE vẫn là `AGENTS.md`, `SCOPE.md` trên branch CODE hiện hành và issue #194.

## Đọc theo thứ tự

1. [01_MA_TRAN_AUDIT.md](01_MA_TRAN_AUDIT.md) — ma trận 17 nhóm chức năng, tách CODE / RUNTIME / dữ liệu thiếu.
2. [02_KICH_BAN_08PHUT30.md](02_KICH_BAN_08PHUT30.md) — kịch bản 8:30, gồm kho, khuyến mãi, báo cáo và các giới hạn.
3. [03_PROMPT_CODEX_TOI_GIAN.md](03_PROMPT_CODEX_TOI_GIAN.md) — giao việc một lần cho Codex.
4. [04_GO_NO_GO.md](04_GO_NO_GO.md) — checklist và fallback.

## Cho Codex trên Windows

Chỉ lấy tài liệu, **không chuyển worktree đang code sang nhánh tài liệu**.

```powershell
cd D:\Projects\KLCN-QuanLyTiemChung
git fetch origin refs/heads/docs/demo-defense-2026-11-17:refs/remotes/origin/docs/demo-defense-2026-11-17
git show origin/docs/demo-defense-2026-11-17:docs/demo-defense-2026-11-17/03_PROMPT_CODEX_TOI_GIAN.md
```

Có thể yêu cầu Codex đọc các file còn lại bằng `git show origin/docs/demo-defense-2026-11-17:docs/demo-defense-2026-11-17/<tên file>`.

**Lưu ý:** Đây là kế hoạch, **không chứng minh** Windows/SQL đã PASS. Script demo, ảnh và log vẫn ở `C:\Users\DAT\Documents\KLCN-Demo-Control\` trên máy người dùng; không đưa mật khẩu, backup SQL, nhật ký nhạy cảm lên GitHub. Không reseed/reset/restore `vac-final`.
