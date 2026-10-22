// Export feature-first classes for backward compatibility
export '../features/catalog/data/models/vaccine_model.dart';
export '../features/catalog/data/repositories/catalog_repository_impl.dart';
export '../features/catalog/domain/models/vaccine.dart';
export '../features/catalog/domain/repositories/catalog_repository.dart';

import 'package:dio/dio.dart';
import '../features/catalog/data/repositories/catalog_repository_impl.dart';
import 'network/dio_client.dart';

/// Legacy alias for CatalogRepositoryImpl with Dio support
class ApiClient extends CatalogRepositoryImpl {
  ApiClient({
    Dio? dio,
    HttpClientAdapter? adapter,
    String? baseUrl,
  }) : super(
          dioClient: DioClient(
            dio: dio,
            baseUrl: baseUrl,
            adapter: adapter,
          ),
        );
}
