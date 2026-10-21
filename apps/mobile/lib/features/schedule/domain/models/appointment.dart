class Appointment {
  const Appointment({
    required this.id,
    required this.patientName,
    required this.vaccineName,
    required this.doseInfo,
    required this.scheduledDate,
    required this.scheduledTime,
    required this.doctorRoom,
    required this.status,
    this.note,
  });

  final int id;
  final String patientName;
  final String vaccineName;
  final String doseInfo;
  final String scheduledDate;
  final String scheduledTime;
  final String doctorRoom;
  final String status; // 'confirmed', 'completed', 'pending'
  final String? note;

  bool get isConfirmed => status == 'confirmed';
  bool get isCompleted => status == 'completed';
}
