// Re-export presentation screen for backward compatibility
export 'presentation/screens/catalog_screen.dart';

import 'presentation/screens/catalog_screen.dart';

class CatalogPage extends CatalogScreen {
  const CatalogPage({super.key, required super.repository});
}
