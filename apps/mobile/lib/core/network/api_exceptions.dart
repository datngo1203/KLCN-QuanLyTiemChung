import 'package:dio/dio.dart';

class ApiException implements Exception {
  const ApiException({
    required this.message,
    this.statusCode,
    this.originalError,
  });

  final String message;
  final int? statusCode;
  final Object? originalError;

  factory ApiException.fromDioException(DioException error) {
    final status = error.response?.statusCode;
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return ApiException(
          message: 'Kết nối mạng quá thời gian chờ (Timeout). Vui lòng thử lại.',
          statusCode: status,
          originalError: error,
        );
      case DioExceptionType.badResponse:
        final detail = error.response?.data;
        if (detail is Map<String, dynamic> && detail.containsKey('detail')) {
          return ApiException(
            message: detail['detail'].toString(),
            statusCode: status,
            originalError: error,
          );
        }
        return ApiException(
          message: 'Không tải được dữ liệu (HTTP $status). Vui lòng thử lại sau.',
          statusCode: status,
          originalError: error,
        );
      case DioExceptionType.connectionError:
        return ApiException(
          message: 'Không thể kết nối đến máy chủ API. Vui lòng kiểm tra mạng hoặc cấu hình API_BASE_URL.',
          statusCode: status,
          originalError: error,
        );
      case DioExceptionType.cancel:
        return ApiException(
          message: 'Yêu cầu kết nối đã bị hủy.',
          statusCode: status,
          originalError: error,
        );
      case DioExceptionType.badCertificate:
        return ApiException(
          message: 'Chứng chỉ bảo mật máy chủ không hợp lệ.',
          statusCode: status,
          originalError: error,
        );
      case DioExceptionType.unknown:
      default:
        return ApiException(
          message: error.message ?? 'Đã xảy ra lỗi không xác định khi kết nối API.',
          statusCode: status,
          originalError: error,
        );
    }
  }

  @override
  String toString() => message;
}
