import 'dart:convert';

import 'package:dio/dio.dart';

import '../../../../core/network/api_endpoints.dart';
import '../../../../core/network/api_exceptions.dart';
import '../../../../core/network/dio_client.dart';
import '../../domain/models/vaccine.dart';
import '../../domain/repositories/catalog_repository.dart';
import '../models/vaccine_model.dart';

class CatalogRepositoryImpl implements CatalogRepository {
  CatalogRepositoryImpl({DioClient? dioClient, Dio? dio})
    : _dioClient =
          dioClient ?? (dio != null ? DioClient(dio: dio) : DioClient());

  final DioClient _dioClient;

  Dio get _dio => _dioClient.dio;

  @override
  Future<List<Vaccine>> getVaccines({String? search}) async {
    try {
      final queryParams = <String, dynamic>{};
      if (search != null && search.trim().isNotEmpty) {
        queryParams['search'] = search.trim();
      }

      final response = await _dio.get(
        ApiEndpoints.vaccines,
        queryParameters: queryParams.isNotEmpty ? queryParams : null,
      );

      if (response.statusCode != 200) {
        throw ApiException(
          message:
              'Không tải được danh mục vắc xin (HTTP ${response.statusCode}).',
          statusCode: response.statusCode,
        );
      }

      dynamic rawData = response.data;
      if (rawData is String) {
        rawData = jsonDecode(rawData);
      }

      if (rawData is! Map<String, dynamic> || rawData['items'] is! List) {
        throw const FormatException('Thiếu danh sách items');
      }
      final source = rawData['source'];
      if (source != null && source is! String) {
        throw const FormatException('Nguồn không hợp lệ');
      }
      final items = (rawData['items'] as List).map((item) {
        if (item is! Map<String, dynamic>) {
          throw const FormatException('Bản ghi không hợp lệ');
        }
        return VaccineModel.fromJson(item, source: source as String?);
      }).toList();
      return CatalogResult(items, source: source as String?);
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiException(
        message: 'Dữ liệu danh mục từ API không hợp lệ. Vui lòng thử lại.',
        originalError: e,
      );
    }
  }

  void close() => _dioClient.close();
}
