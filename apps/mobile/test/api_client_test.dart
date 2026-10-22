import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tiem_chung_mobile/core/api_client.dart';

class MockDioAdapter implements HttpClientAdapter {
  MockDioAdapter(this.handler);

  final Future<ResponseBody> Function(RequestOptions options) handler;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) {
    return handler(options);
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  test('Uses agreed API path and parses decimal price', () async {
    final api = ApiClient(
      baseUrl: 'http://localhost:5080/',
      adapter: MockDioAdapter((options) async {
        expect(options.path, '/api/v1/catalog/vaccines');
        return ResponseBody.fromString(
          '{"source":"demo","items":[{"id":1,"name":"A","price":450000.5}]}',
          200,
          headers: {
            Headers.contentTypeHeader: [Headers.jsonContentType],
          },
        );
      }),
    );
    addTearDown(api.close);
    expect((await api.getVaccines()).single.price, 450000.5);
  });

  test('Failed HTTP request is not treated as empty success', () async {
    final api = ApiClient(
      adapter: MockDioAdapter((options) async {
        return ResponseBody.fromString(
          'Unavailable',
          503,
          statusMessage: 'Service Unavailable',
        );
      }),
    );
    addTearDown(api.close);
    await expectLater(api.getVaccines(), throwsException);
  });
  for (final payload in [
    '{}',
    '[]',
    '{"items":null}',
    '{"items":[null]}',
    '{"items":[{"id":"1","name":"A"}]}',
    '{"items":[{"id":1,"name":""}]}',
    '{"items":[{"id":1,"name":"A","price":-1}]}',
    '{"items":[{"id":1,"name":"A","origin":42}]}',
    'not json',
  ]) {
    test('Rejects malformed payload: $payload', () async {
      final api = ApiClient(
        adapter: MockDioAdapter(
          (_) async => ResponseBody.fromString(payload, 200),
        ),
      );
      addTearDown(api.close);
      await expectLater(api.getVaccines(), throwsException);
    });
  }
  test(
    'Missing optional fields stay unknown; medical extras are not trusted',
    () {
      final v = VaccineModel.fromJson({
        'id': 1,
        'name': 'HPV',
        'inStock': true,
        'scheduleSummary': 'invented',
      });
      expect(v.price, isNull);
      expect(v.manufacturer, isNull);
      expect(v.origin, isNull);
      expect(v.inStock, isNull);
      expect(v.scheduleSummary, isNull);
      expect(v.targetGroup, isNull);
      expect(v.diseasePrevention, isNull);
      expect(v.formattedPrice, 'Chưa có giá');
    },
  );
  for (final source in ['demo', 'database', 'other']) {
    test('Preserves source for empty results: $source', () async {
      final api = ApiClient(
        adapter: MockDioAdapter(
          (_) async =>
              ResponseBody.fromString('{"source":"$source","items":[]}', 200),
        ),
      );
      addTearDown(api.close);
      final result = await api.getVaccines();
      expect(result, isEmpty);
      expect((result as CatalogResult).source, source);
    });
  }
  test('Network timeout is an error without sample fallback', () async {
    final api = ApiClient(
      adapter: MockDioAdapter(
        (options) async => throw DioException(
          requestOptions: options,
          type: DioExceptionType.connectionTimeout,
        ),
      ),
    );
    addTearDown(api.close);
    await expectLater(api.getVaccines(), throwsException);
  });
}
