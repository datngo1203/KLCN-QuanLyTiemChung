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
    : _dioClient = dioClient ?? (dio != null ? DioClient(dio: dio) : DioClient());

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
          message: 'Không tải được danh mục vắc xin (HTTP ${response.statusCode}).',
          statusCode: response.statusCode,
        );
      }

      dynamic rawData = response.data;
      if (rawData is String) {
        rawData = jsonDecode(rawData);
      }

      List<dynamic> itemsList;
      if (rawData is Map<String, dynamic> && rawData.containsKey('items')) {
        itemsList = rawData['items'] as List<dynamic>;
      } else if (rawData is List<dynamic>) {
        itemsList = rawData;
      } else {
        itemsList = [];
      }

      return itemsList
          .map((item) => VaccineModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiException(
        message: 'Lỗi xử lý dữ liệu: $e',
        originalError: e,
      );
    }
  }

  void close() => _dioClient.close();
}
