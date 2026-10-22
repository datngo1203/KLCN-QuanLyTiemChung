abstract final class AppConstants {
  static const String appName = 'Trung tâm Tiêm chủng';
  static const String appTagline = 'Hệ thống Quản lý Tiêm chủng An toàn & Toàn diện';

  static const String defaultApiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5080',
  );

  static const Duration connectTimeout = Duration(seconds: 8);
  static const Duration receiveTimeout = Duration(seconds: 8);

  static const List<String> catalogCategories = [
    'Tất cả',
    'Trẻ em',
    'Người lớn',
    'Phụ nữ dự định/mang thai',
    'Gói vắc xin',
  ];
}
