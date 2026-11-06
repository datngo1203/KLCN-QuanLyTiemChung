# Web rules — KLCN090

Áp dụng cho `apps/web/**`. Root `AGENTS.md`, `SCOPE.md` và active issue vẫn là source-of-truth.

## Product / reference
- Web là customer-facing React client, gọi API thật; không đọc SQL, không thêm Sample fallback.
- Khi task là UI/UX, tham khảo đúng trang công khai VNVC/vax.vnvc.vn tương ứng nghiệp vụ. Chỉ tái tạo ngôn ngữ thị giác/tương tác phù hợp KLCN; không copy nguyên code/assets không được phép và không bịa API/quy trình nội bộ.
- Theo quyết định dự án, presentation có thể dùng tên “Trung tâm tiêm chủng VNVC” và logo người dùng cung cấp. Không tự chèn disclaimer runtime chỉ để tách thương hiệu.
- Với UI task đáng kể, kiểm skill UI hiện có và dùng theo SKILL GATE của prompt; prototype không thay React thật.

## UX
- Ưu tiên task flow dễ hiểu, responsive, semantic status và wording nghiệp vụ; không lộ raw enum/provider/internal error.
- Phân biệt rõ preference/yêu cầu của khách với state đã được trung tâm xác nhận khi active issue yêu cầu.
- Không derive security/business permission chỉ từ UI; server là nguồn quyết định.
- Dữ liệu tiền hiển thị VND nhất quán; input tiền theo convention dự án hiện hành.

## Gates
- Test component/unit liên quan + lint/build.
- UI change cần browser smoke trên desktop và mobile viewport phù hợp.
- Kiểm loading/empty/error/permission/submit/retry, giữ draft/input khi lỗi nếu contract yêu cầu.
- Build xanh nhưng chưa browser/runtime thì ghi `UNVERIFIED`.
