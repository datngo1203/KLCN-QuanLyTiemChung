import 'dart:collection';

import '../models/vaccine.dart';

abstract interface class CatalogRepository {
  Future<List<Vaccine>> getVaccines({String? search});
}

class CatalogResult extends ListBase<Vaccine> {
  CatalogResult(List<Vaccine> items, {this.source})
    : _items = List.unmodifiable(items);
  final List<Vaccine> _items;
  final String? source;
  String get sourceLabel => source == 'demo'
      ? 'API: Dữ liệu mẫu'
      : source == 'database'
      ? 'API: Cơ sở dữ liệu'
      : 'API: Nguồn chưa xác định';
  @override
  int get length => _items.length;
  @override
  set length(int value) => throw UnsupportedError('read only');
  @override
  Vaccine operator [](int index) => _items[index];
  @override
  void operator []=(int index, Vaccine value) =>
      throw UnsupportedError('read only');
}
