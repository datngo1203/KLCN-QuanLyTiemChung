class PatientProfile {
  const PatientProfile({
    required this.id,
    required this.fullName,
    required this.relation,
    required this.dateOfBirth,
    required this.gender,
    required this.vaccinationCode,
    this.bloodType = 'Chưa xác định',
    this.allergyNotes = 'Không có tiền sử dị ứng ghi nhận',
    this.completedShotsCount = 0,
  });

  final int id;
  final String fullName;
  final String relation;
  final String dateOfBirth;
  final String gender;
  final String vaccinationCode;
  final String bloodType;
  final String allergyNotes;
  final int completedShotsCount;
}
