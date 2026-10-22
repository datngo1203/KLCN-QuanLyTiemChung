import 'package:dio/dio.dart';
import '../constants/app_constants.dart';

class DioClient {
  DioClient({
    String? baseUrl,
    Dio? dio,
    HttpClientAdapter? adapter,
    List<Interceptor>? interceptors,
  }) : _baseUrl = baseUrl ?? AppConstants.defaultApiBaseUrl {
    if (dio != null) {
      _dio = dio;
    } else {
      final base = _baseUrl.replaceAll(RegExp(r'/$'), '');
      _dio = Dio(
        BaseOptions(
          baseUrl: base,
          connectTimeout: AppConstants.connectTimeout,
          receiveTimeout: AppConstants.receiveTimeout,
          headers: const {
            'Content-Type': 'application/json',
            'Accept': 'application/json',
          },
        ),
      );
      if (adapter != null) {
        _dio.httpClientAdapter = adapter;
      }
    }

    if (interceptors != null && interceptors.isNotEmpty) {
      _dio.interceptors.addAll(interceptors);
    }
  }

  final String _baseUrl;
  late final Dio _dio;

  Dio get dio => _dio;
  String get baseUrl => _dio.options.baseUrl.isNotEmpty ? _dio.options.baseUrl : _baseUrl;

  void close({bool force = false}) {
    _dio.close(force: force);
  }
}
