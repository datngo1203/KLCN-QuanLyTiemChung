import 'package:flutter/material.dart';

import '../../core/api_client.dart';

class CatalogPage extends StatefulWidget {
  const CatalogPage({super.key, required this.repository});
  final CatalogRepository repository;
  @override
  State<CatalogPage> createState() => _CatalogPageState();
}

class _CatalogPageState extends State<CatalogPage> {
  late Future<List<Vaccine>> _items;
  @override
  void initState() {
    super.initState();
    _items = widget.repository.getVaccines();
  }

  void _reload() => setState(() => _items = widget.repository.getVaccines());
  @override
  Widget build(BuildContext context) => FutureBuilder<List<Vaccine>>(
    future: _items,
    builder: (context, snapshot) {
      if (snapshot.connectionState != ConnectionState.done) {
        return const Center(child: CircularProgressIndicator());
      }
      if (snapshot.hasError) {
        return Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text('Chưa tải được danh mục. Kiểm tra kết nối API.'),
                const SizedBox(height: 12),
                FilledButton(onPressed: _reload, child: const Text('Thử lại')),
              ],
            ),
          ),
        );
      }
      final items = snapshot.data ?? [];
      if (items.isEmpty) return const Center(child: Text('Chưa có vắc xin.'));
      return ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: items.length,
        separatorBuilder: (context, index) => const SizedBox(height: 8),
        itemBuilder: (context, index) => Card(
          child: ListTile(
            leading: const Icon(Icons.vaccines_outlined),
            title: Text(items[index].name),
            subtitle: Text('${items[index].price.toStringAsFixed(0)} đ'),
          ),
        ),
      );
    },
  );
}
