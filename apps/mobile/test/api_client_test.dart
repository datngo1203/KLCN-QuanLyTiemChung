import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:tiem_chung_mobile/core/api_client.dart';

void main() {
  test('Uses agreed API path and parses decimal price', () async {
    final api = ApiClient(
      baseUrl: 'http://localhost:5080/',
      client: MockClient((request) async {
        expect(request.url.path, '/api/v1/catalog/vaccines');
        return http.Response(
          '{"source":"demo","items":[{"id":1,"name":"A","price":450000.5}]}',
          200,
        );
      }),
    );
    addTearDown(api.close);
    expect((await api.getVaccines()).single.price, 450000.5);
  });
  test('Failed HTTP request is not treated as empty success', () async {
    final api = ApiClient(
      client: MockClient((_) async => http.Response('Unavailable', 503)),
    );
    addTearDown(api.close);
    await expectLater(api.getVaccines(), throwsException);
  });
}
