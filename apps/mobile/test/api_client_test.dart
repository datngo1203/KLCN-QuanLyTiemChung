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
}
