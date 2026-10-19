# Mobile — Quý phụ trách

Flutter 3.47.7 stable, Dart 3.13.5; khung Android/iOS. Có navigation, API client, màn danh mục gọi API và test. Lịch/hồ sơ chờ module nghiệp vụ.

```powershell
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080
flutter analyze
flutter test
```

Đọc ../../docs/setup.md cho emulator/thiết bị thật/iOS. Module Identity chưa có; không lưu token trong mã và không gọi SQL trực tiếp. iOS phải build/ký trên macOS. Chưa có ký phát hành, push hoặc thanh toán thật.
