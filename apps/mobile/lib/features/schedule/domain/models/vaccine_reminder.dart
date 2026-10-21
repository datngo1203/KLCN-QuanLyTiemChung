class VaccineReminder {
  const VaccineReminder({
    required this.id,
    required this.patientName,
    required this.vaccineName,
    required this.dose,
    required this.targetDate,
    required this.daysRemaining,
    this.channel = 'Thông báo App',
    this.isEnabled = true,
  });

  final int id;
  final String patientName;
  final String vaccineName;
  final String dose;
  final String targetDate;
  final int daysRemaining;
  final String channel;
  final bool isEnabled;

  VaccineReminder copyWith({bool? isEnabled, String? channel}) {
    return VaccineReminder(
      id: id,
      patientName: patientName,
      vaccineName: vaccineName,
      dose: dose,
      targetDate: targetDate,
      daysRemaining: daysRemaining,
      channel: channel ?? this.channel,
      isEnabled: isEnabled ?? this.isEnabled,
    );
  }
}
