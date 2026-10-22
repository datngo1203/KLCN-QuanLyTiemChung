class Vaccine {
  const Vaccine({
    required this.id,
    required this.name,
    required this.price,
    this.manufacturer,
    this.origin,
    this.diseasePrevention,
    this.targetGroup,
    this.scheduleSummary,
    this.inStock = true,
  });

  final int id;
  final String name;
  final double price;
  final String? manufacturer;
  final String? origin;
  final String? diseasePrevention;
  final String? targetGroup;
  final String? scheduleSummary;
  final bool inStock;

  /// Format price in Vietnamese Dong (e.g. 450.000 đ)
  String get formattedPrice {
    final intPrice = price.round();
    final str = intPrice.toString();
    final buffer = StringBuffer();
    int count = 0;
    for (int i = str.length - 1; i >= 0; i--) {
      buffer.write(str[i]);
      count++;
      if (count % 3 == 0 && i != 0) {
        buffer.write('.');
      }
    }
    return '${buffer.toString().split('').reversed.join()} đ';
  }
}
