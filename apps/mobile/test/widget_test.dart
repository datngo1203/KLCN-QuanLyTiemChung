import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tiem_chung_mobile/app.dart';
import 'package:tiem_chung_mobile/core/api_client.dart';

class FakeCatalog implements CatalogRepository {
  @override
  Future<List<Vaccine>> getVaccines() async => [
    const Vaccine(id: 1, name: 'Vắc xin mẫu A', price: 450000),
  ];
}

void main() {
  testWidgets('Loads API catalog and navigates without network', (
    tester,
  ) async {
    await tester.pumpWidget(TiemChungApp(repository: FakeCatalog()));
    await tester.pumpAndSettle();
    expect(find.text('Vắc xin mẫu A'), findsOneWidget);
    await tester.tap(find.byIcon(Icons.calendar_month_outlined));
    await tester.pumpAndSettle();
    expect(find.textContaining('Lịch hẹn sẽ có'), findsOneWidget);
  });
}
