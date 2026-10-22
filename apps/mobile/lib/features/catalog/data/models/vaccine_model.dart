import '../../domain/models/vaccine.dart';

class VaccineModel extends Vaccine {
  const VaccineModel({
    required super.id,
    required super.name,
    super.price,
    super.manufacturer,
    super.origin,
    super.source,
  });
  factory VaccineModel.fromJson(Map<String, dynamic> json, {String? source}) {
    final id = json['id'];
    final name = json['name'];
    final price = json['price'];
    if (id is! int ||
        id <= 0 ||
        name is! String ||
        name.trim().isEmpty ||
        (price != null && (price is! num || !price.isFinite || price < 0))) {
      throw const FormatException('Bản ghi danh mục không hợp lệ');
    }
    String? text(String key) {
      final value = json[key];
      if (value == null) return null;
      if (value is! String) throw FormatException('Trường $key không hợp lệ');
      return value.trim().isEmpty ? null : value.trim();
    }

    return VaccineModel(
      id: id,
      name: name.trim(),
      price: (price as num?)?.toDouble(),
      manufacturer: text('manufacturer'),
      origin: text('origin'),
      source: source,
    );
  }
}
