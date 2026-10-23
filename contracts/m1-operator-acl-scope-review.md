# Điều chỉnh hẹp ACL deployment operator — chờ review trước code

Base `48171fe` (CheckChain không đổi từ `517211c`). Không thay batch fix, SQL/auth, mail hoặc quota recovery. Chưa sửa code bảo mật/OS/ACL/deployment. Nội dung này đề nghị thay điều kiện ancestor của [contract operator](m1-p1-operator-delivery-review.md), không phải approval triển khai.

## Kết luận từ khảo sát chỉ đọc

CheckChain hiện **từ chối Windows deployment hợp lệ quá rộng**: whitelist owner SYSTEM/Administrators cho mọi ancestor, bắt mọi DACL protected và gộp mọi allow-write không xét inherit-only/deny/object type. Nó không phân biệt tạo sibling với thay thế thành phần đường dẫn. Elevation không thay ACL đó; không cần dựng VM chỉ để làm checker cũ pass.

ACL thật trên máy hiện tại:

| Thành phần | Quan sát | Ý nghĩa giới hạn |
|---|---|---|
| C:\ | owner TrustedInstaller SID `S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464`, DACL không protected; AU allow `0x4` hiệu lực và `0xE0010000` OI/CI/inherit-only | `0x4` trên directory tạo subdirectory mới, không tự cho sửa/xóa child có sẵn. ACE inherit-only không cấp quyền ngay trên root. Không được kết luận root bị caller chiếm chỉ từ hai ACE này |
| C:\ProgramData | SYSTEM/protected; Users allow `0x116` CI, CREATOR OWNER generic-all inherit-only | Có create-file/subdirectory và write-EA/attributes hiệu lực trên directory. Không được coi toàn bộ “Write” là vô hại hoặc tự suy quyền đó cho policy child có protected DACL. Cần phân tích thêm quyền đổi metadata/reparse; gói tối thiểu dưới đây không dùng ancestor này |
| C:\Program Files | TrustedInstaller/protected; Users RX hiệu lực, generic RX inherit-only; TrustedInstaller full-control | Không thấy allow-write hiệu lực cho Users trong SDDL khảo sát; đây là candidate ancestor, không phải bằng chứng positive deployment. App folder chưa tồn tại |

Không thử tamper/rename/reparse trên các đường hệ thống, không dùng write-open làm phép thử. Việc dịch một số tên capability SID qua .NET thất bại; đã đọc raw SDDL, không suy “không dịch được tên” là quyền an toàn.

## Thiết kế tối thiểu đề nghị chốt

1. **Đường cố định mới:** native Known Folder **ProgramFilesX64** (trên máy x64 này là `C:\Program Files`), default path không env/redirection; `KLCN090\IdentityOperator\policy.json` và `bin` bên dưới cùng app root. Không fallback ProgramData/CWD hoặc tham số override. Chuyển policy location là thay contract cần review, không âm thầm đổi code. Không runtime dev/test bypass.
2. **App subtree:** root KLCN090, IdentityOperator, bin, policy, binary và mọi dependency executable/config phải có owner SYSTEM/Administrators, protected DACL, chỉ admin/SYSTEM được mutate; SID operator read/read-execute, không write/delete/change-DACL/take-owner. Không CREATOR OWNER write kế thừa bên trong app. Admin precreate toàn bộ, không để operator/caller tạo phần còn thiếu. Thiếu thành phần hoặc preplanted object do caller sở hữu → từ chối, không tự sửa/tiếp quản.
3. **Ancestor toàn chuỗi vẫn kiểm:** từ volume root đến app subtree, owner có thể là SYSTEM/Administrators hoặc **đúng SID TrustedInstaller** trên Windows servicing ancestor; không chấp nhận service SID tùy ý. Không bắt ancestor hệ thống protected chỉ để đẹp cấu hình. Kiểm quyền có hiệu lực trên chính từng object và từng cạnh parent→child đã tồn tại; app subtree vẫn bắt protected. Không cần thay owner/DACL của C:\ hoặc Program Files.
4. **Các quyền nguy hiểm phải chặn cho caller không đặc quyền:** WRITE_DAC/WRITE_OWNER hoặc owner không tin cậy trên bất kỳ component; DELETE trên child **hoặc** FILE_DELETE_CHILD trên parent cho phép xóa/thay thành phần đường dẫn; write-data/append trên policy/binary/dependency; write-EA/attributes/reparse-capable mutation trên component tin cậy. Với ancestor directory, FILE_ADD_FILE/FILE_ADD_SUBDIRECTORY riêng chỉ tạo sibling, không phải write-data vào existing child. Chỉ cho phép quyền tạo sibling nếu child đã tồn tại, child không thể bị untrusted sửa/xóa/đổi owner/DACL và parent không có untrusted delete-child. Thiếu child → từ chối trước đọc secret/kết nối SQL. Không whitelist toàn GENERIC_WRITE/Modify.
5. **ACE scope và deny:** map generic rights theo file/directory; inherit-only bỏ khỏi quyền ngay trên object nhưng kiểm ảnh hưởng tại child bằng DACL thực của child. CREATOR OWNER được xét ở owner/child đã materialize, không cấp quyền trên parent vì placeholder đó. Không union mọi Allow hoặc dùng quy tắc “deny luôn thắng” bất chấp thứ tự: dùng Windows access-check semantics có SID groups/enabled/deny-only, thứ tự ACE và owner rights. Với các broad trustee hoặc unresolved trustee trong ACL nhỏ của đường cố định, kiểm bảo thủ; nếu không chứng minh được quyền mutation bị chặn thì fail closed. Không dùng chỉ token operator hiện tại để tuyên bố mọi tài khoản khác đều không thể ghi. Chỉ hỗ trợ DACL ACE thông thường cần cho đường này; callback/conditional/object-specific không hiểu → từ chối. Không xây ACL engine tổng quát hoặc role editor.
6. **Reparse và race:** mở/check component không follow reparse (kể cả ancestor), kiểm local volume/identity thực thay vì chuỗi tên; giữ handles cần thiết chống delete/replace khi đọc policy, đọc qua chính handle đã kiểm, revalidate file identity/DACL trước sử dụng nếu phải reopen. Không `Get-Acl(path)` rồi `ReadAllText(path)` qua một resolution khác mà bỏ qua race. App/file không writable, parent delete-child bị chặn cũng là invariant; quyền system admin thay ACL không nằm trong threat model. Không cấp privilege hay thêm test bypass để tạo đường dương.
7. **OS/SQL giữ nguyên:** WindowsIdentity SID token thật + expected secret từ policy bảo vệ; recovery manifest trong policy, actor DB credential cho lệnh thường, Integrated SQL đích cố định. Không tin env/path caller. Không hứa chống OS/DB admin, TrustedInstaller servicing hoặc principal vận hành đã bị chiếm có quyền SQL trực tiếp.

**Đường tấn công phải bị chặn cụ thể:** child protected nhưng parent cấp Users FILE_DELETE_CHILD → caller có thể xóa/đổi tên child rồi tạo replacement policy/junction; child owner untrusted → caller sửa DACL rồi nội dung; inherit-only grant materialize thành write ở binary child → sửa binary; policy tồn tại ở reparse redirect → đọc secret/DB target của caller. Ngược lại, chỉ tạo `C:\OtherFolder` khi `C:\Program Files` có sẵn và không thể bị thay thế không chứng minh takeover đường Program Files. Không tuyên bố đã chứng minh một đường takeover thành công trên máy này.

## Regression cần bổ sung sau chấp thuận

- Positive trên ACL Windows test riêng được cấp phép: root TrustedInstaller/unprotected với create-sibling-only; Program Files ancestor Users RX và ACE inherit-only; app subtree protected đúng owner; bootstrap/recovery/reset CLI vẫn hoạt động. Không sửa root máy thật để tạo fixture. Cùng token thực kiểm denial mutation trên target, không chỉ checker trả true.
- Negative theo từng cạnh: parent delete-child dù child deny DELETE; child DELETE; file write/append; ancestor write-DAC/owner; owner caller; missing/preplanted app; inherited write ở dependency; CREATOR OWNER materialized cho caller. Test broad-group grants, deny-before-allow/allow-before-deny và deny-only SID, không giả Allow union. ACL quá phức tạp phải refuse.
- Race/reparse: junction/symlink ở mỗi cấp; đổi tên/replace policy giữa check/read trên fixture riêng phải không đọc replacement; không đổi ACL/cài junction hệ thống.
- Env ProgramFiles/ProgramData/HOME/path giả, user-writable checkout binary và policy caller tạo phải không thành trusted deployment. SID display-name trùng không thay SID thực.
- SQL/auth tests hiện có vẫn chạy; positive Windows deployment evidence ghi OS/token/owner/DACL/hash/DB/audit/ledger/login sạch secrets. Unit fake và CI Linux chỉ kiểm phần tương ứng, không đóng positive CLI.

## Provisioning thực sự còn cần quyền, chưa cho phép làm

Không cần VM, tài khoản OS mới hoặc sửa root/ProgramData ACL theo đề xuất này. Nếu review chấp thuận **và người dùng cho phép provision riêng sau đó**, admin chỉ tạo app root mới dưới Program Files và files bên trong, đặt owner/protected DACL trong subtree; nếu đã có folder không đúng ownership thì dừng, không ghi đè. Giữ ACL ancestor hệ thống nguyên trạng. Xóa/khôi phục chỉ artifacts mới theo manifest, không policy có trước.

OS principal có thể là SID của tài khoản Windows hiện hữu chạy không elevated, nếu quản trị chấp thuận identity đó; không tự tạo user/group. Đây là principal vận hành có SQL DML tương ứng nên không hứa tách bảo vệ khỏi chính người dùng đó. Admin cài policy giữ secret/binary đúng ACL và cấp read cần thiết, không cấp quyền ghi app folder cho SID operator.

DBA chỉ tạo hai DB synthetic clean/legacy và map Integrated SID vào đúng hai DB với quyền tối thiểu cho operator; migration bằng principal deployment riêng. Không cấp server-wide/sysadmin/dbcreator/db_owner hoặc đổi DB/grants dùng chung. SQL instance hiện hữu chỉ được dùng nếu DBA cho phép tạo DB test/grants giới hạn và TLS phù hợp; nếu không thì positive SQL/CLI vẫn chặn, không tự cài SQL/đổi TrustServerCertificate.

Hiện chỉ có kết luận checker quá chặt và candidate path từ khảo sát ACL; chưa có approval contract mới/provisioning, chưa có ACL effective access/CLI positive thực. Chưa chứng minh máy này bất khả thi, cũng chưa gọi an toàn/đủ nghiệm thu. Cần reviewer kiểm diff này trước code thay CheckChain và trước đề nghị quyền provision cụ thể.

## Nguồn kỹ thuật

- [Microsoft File Security](https://learn.microsoft.com/en-us/windows/win32/fileio/file-security-and-access-rights): quyền trên child/DACL của child và inheritance; parent ACL không tự cấp nội dung file con.
- [Microsoft access-right constants](https://learn.microsoft.com/en-us/windows/win32/fileio/file-access-rights-constants): create-subdirectory khác delete-child/file-write.
- [Microsoft AccessCheck](https://learn.microsoft.com/en-us/windows/win32/secauthz/how-dacls-control-access-to-an-object): token/group và thứ tự Allow/Deny.
- [Microsoft inheritance](https://learn.microsoft.com/en-us/windows/win32/secauthz/ace-inheritance-rules): inherit-only và CREATOR OWNER mapping.
