import '../../domain/models/vaccine.dart';

class VaccineModel extends Vaccine {
  const VaccineModel({
    required super.id,
    required super.name,
    required super.price,
    super.manufacturer,
    super.origin,
    super.diseasePrevention,
    super.targetGroup,
    super.scheduleSummary,
    super.inStock,
  });

  factory VaccineModel.fromJson(Map<String, dynamic> json) {
    final id = json['id'] as int;
    final name = (json['name'] ?? 'Vắc xin #${json['id']}') as String;
    final price = (json['price'] as num?)?.toDouble() ?? 0.0;
    final manufacturer = json['manufacturer'] as String?;
    final origin = json['origin'] as String?;

    // Provide helpful clinical context based on vaccine name
    final disease = _inferDisease(name);
    final targetGroup = _inferTargetGroup(name);
    final schedule = _inferSchedule(name);

    return VaccineModel(
      id: id,
      name: name,
      price: price,
      manufacturer: manufacturer ?? 'Trung tâm Tiêm chủng',
      origin: origin ?? 'Việt Nam',
      diseasePrevention: disease,
      targetGroup: targetGroup,
      scheduleSummary: schedule,
      inStock: true,
    );
  }

  static String _inferDisease(String name) {
    final lower = name.toLowerCase();
    if (lower.contains('cúm')) return 'Phòng ngừa các chủng cúm mùa nguy cơ cao (A/H1N1, A/H3N2, B)';
    if (lower.contains('phế cầu')) return 'Phòng viêm phổi, viêm màng não do phế cầu khuẩn Streptococcus pneumoniae';
    if (lower.contains('6 trong 1') || lower.contains('hexa')) return 'Bạch hầu, ho gà, uốn ván, bại liệt, Hib, viêm gan B';
    if (lower.contains('hpv') || lower.contains('ung thư')) return 'Ung thư cổ tử cung, u nhú sinh dục do HPV tuýp 6, 11, 16, 18';
    if (lower.contains('thủy đậu')) return 'Bệnh thủy đậu (Varicella) và biến chứng zona thần kinh';
    if (lower.contains('sởi')) return 'Phòng sởi, quai bị, rubella (MMR)';
    return 'Phòng ngừa các bệnh truyền nhiễm nguy hiểm theo phác đồ Bộ Y Tế';
  }

  static String _inferTargetGroup(String name) {
    final lower = name.toLowerCase();
    if (lower.contains('trẻ') || lower.contains('6 trong 1')) return 'Trẻ từ 2 tháng đến 24 tháng tuổi';
    if (lower.contains('hpv')) return 'Nữ giới và nam giới từ 9 đến 45 tuổi';
    if (lower.contains('cúm')) return 'Trẻ từ 6 tháng tuổi, người lớn và người cao tuổi';
    return 'Trẻ em và người lớn theo chỉ định khám sàng lọc';
  }

  static String _inferSchedule(String name) {
    final lower = name.toLowerCase();
    if (lower.contains('cúm')) return 'Tiêm nhắc lại hằng năm (1 mũi/năm)';
    if (lower.contains('hpv')) return 'Phác đồ 2-3 mũi tùy theo độ tuổi khi bắt đầu tiêm';
    if (lower.contains('6 trong 1')) return '3 mũi cơ bản lúc 2, 3, 4 tháng và 1 mũi nhắc lại lúc 16-18 tháng';
    return 'Theo phác đồ tiêm chủng chuẩn và tư vấn của Bác sĩ sàng lọc';
  }
}
