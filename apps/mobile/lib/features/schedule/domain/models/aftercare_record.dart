class AftercareRecord {
  const AftercareRecord({
    required this.id,
    required this.patientName,
    required this.vaccineName,
    required this.injectionTime,
    this.observationDurationMinutes = 30,
    required this.status, // 'observing', 'cleared', 'home_monitoring'
    this.temperature = 36.8,
    this.symptoms = const [],
    this.doctorNotes = 'Sức khỏe ổn định, không có dấu hiệu phản vệ cấp tính.',
  });

  final int id;
  final String patientName;
  final String vaccineName;
  final DateTime injectionTime;
  final int observationDurationMinutes;
  final String status;
  final double temperature;
  final List<String> symptoms;
  final String doctorNotes;

  bool get isObservingAtClinic => status == 'observing';
  bool get isCleared => status == 'cleared';
  bool get isHomeMonitoring => status == 'home_monitoring';

  int get elapsedMinutes {
    final diff = DateTime.now().difference(injectionTime).inMinutes;
    return diff.clamp(0, observationDurationMinutes);
  }

  int get remainingMinutes => (observationDurationMinutes - elapsedMinutes).clamp(0, observationDurationMinutes);
  double get progress => (elapsedMinutes / observationDurationMinutes).clamp(0.0, 1.0);
}
