import 'package:flutter/material.dart';

import 'core/api_client.dart';
import 'features/catalog/catalog_page.dart';

class TiemChungApp extends StatelessWidget {
  const TiemChungApp({super.key, this.repository});
  final CatalogRepository? repository;
  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'Tiêm chủng',
    debugShowCheckedModeBanner: false,
    theme: ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xff2563eb)),
      scaffoldBackgroundColor: const Color(0xfff8f9ff),
    ),
    home: HomePage(repository: repository),
  );
}

class HomePage extends StatefulWidget {
  const HomePage({super.key, this.repository});
  final CatalogRepository? repository;
  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  int _index = 0;
  ApiClient? _ownedClient;
  late final CatalogRepository _repository;
  @override
  void initState() {
    super.initState();
    _repository = widget.repository ?? (_ownedClient = ApiClient());
  }

  @override
  void dispose() {
    _ownedClient?.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Tiêm chủng')),
    body: IndexedStack(
      index: _index,
      children: [
        CatalogPage(repository: _repository),
        const FeatureShell(
          icon: Icons.calendar_month,
          title: 'Lịch tiêm',
          detail: 'Lịch hẹn sẽ có sau khi kết nối hồ sơ và đăng nhập.',
        ),
        const FeatureShell(
          icon: Icons.person_outline,
          title: 'Hồ sơ',
          detail: 'Hồ sơ và đăng nhập đang được phát triển.',
        ),
      ],
    ),
    bottomNavigationBar: NavigationBar(
      selectedIndex: _index,
      onDestinationSelected: (value) => setState(() => _index = value),
      destinations: const [
        NavigationDestination(
          icon: Icon(Icons.vaccines_outlined),
          label: 'Danh mục',
        ),
        NavigationDestination(
          icon: Icon(Icons.calendar_month_outlined),
          label: 'Lịch tiêm',
        ),
        NavigationDestination(icon: Icon(Icons.person_outline), label: 'Hồ sơ'),
      ],
    ),
  );
}

class FeatureShell extends StatelessWidget {
  const FeatureShell({
    super.key,
    required this.icon,
    required this.title,
    required this.detail,
  });
  final IconData icon;
  final String title;
  final String detail;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 48),
          const SizedBox(height: 16),
          Text(title, style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          Text(detail, textAlign: TextAlign.center),
        ],
      ),
    ),
  );
}
