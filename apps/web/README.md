# Website — Bảo phụ trách

React 19 + Vite 8. Kế thừa UI trước đây, thêm src/api/client.js và ApiProbe.jsx gọi API thật qua VITE_API_BASE_URL. Form còn dùng state cục bộ; chưa lưu SQL hoặc đăng nhập thật. Tách dần App.jsx theo feature khi triển khai.

```powershell
npm ci
npm run dev
npm run lint
npm run build
```

Xem ../../docs/setup.md và ../../contracts/README.md. Không đưa secret vào biến VITE_*.
