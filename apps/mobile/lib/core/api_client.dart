import 'dart:convert';

import 'package:http/http.dart' as http;

class Vaccine {
  const Vaccine({required this.id, required this.name, required this.price});
  factory Vaccine.fromJson(Map<String, dynamic> json) => Vaccine(
    id: json['id'] as int,
    name: json['name'] as String,
    price: (json['price'] as num).toDouble(),
  );
  final int id;
  final String name;
  final double price;
}

abstract interface class CatalogRepository {
  Future<List<Vaccine>> getVaccines();
}

class ApiClient implements CatalogRepository {
  ApiClient({http.Client? client, String? baseUrl})
    : _client = client ?? http.Client(),
      _baseUrl =
          baseUrl ??
          const String.fromEnvironment(
            'API_BASE_URL',
            defaultValue: 'http://10.0.2.2:5080',
          );
  final http.Client _client;
  final String _baseUrl;
  @override
  Future<List<Vaccine>> getVaccines() async {
    final base = _baseUrl.replaceAll(RegExp(r'/$'), '');
    final response = await _client
        .get(Uri.parse('$base/api/v1/catalog/vaccines'))
        .timeout(const Duration(seconds: 8));
    if (response.statusCode != 200) {
      throw Exception('Không tải được danh mục (HTTP ${response.statusCode}).');
    }
    final body =
        jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>;
    return (body['items'] as List)
        .map((item) => Vaccine.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  void close() => _client.close();
}
