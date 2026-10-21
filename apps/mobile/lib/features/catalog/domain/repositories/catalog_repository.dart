import '../models/vaccine.dart';

abstract interface class CatalogRepository {
  Future<List<Vaccine>> getVaccines({String? search});
}
