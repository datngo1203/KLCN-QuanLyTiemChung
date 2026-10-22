import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../domain/models/aftercare_record.dart';
import '../../domain/models/appointment.dart';
import '../../domain/models/vaccine_reminder.dart';
import '../widgets/aftercare_card.dart';
import '../widgets/appointment_card.dart';
import '../widgets/reminder_card.dart';

class ScheduleScreen extends StatefulWidget {
  const ScheduleScreen({super.key});

  @override
  State<ScheduleScreen> createState() => _ScheduleScreenState();
}

class _ScheduleScreenState extends State<ScheduleScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabController;

  static const List<Appointment> _upcomingAppointments = [
    Appointment(
      id: 101,
      patientName: 'Bé Nguyễn Gia Hưng (2 tuổi)',
      vaccineName: 'Hexaxim 6 trong 1',
      doseInfo: 'Mũi 4 (Nhắc lại)',
      scheduledDate: '24/10/2026',
      scheduledTime: '09:00',
      doctorRoom: 'Phòng tiêm 02 - Tầng 1',
      status: 'confirmed',
      note: 'Vui lòng có mặt trước 15 phút để khám sàng lọc.',
    ),
    Appointment(
      id: 102,
      patientName: 'Nguyễn Ngọc Quý',
      vaccineName: 'Vaxigrip Tetra (Cúm mùa)',
      doseInfo: 'Mũi nhắc 2026',
      scheduledDate: '15/11/2026',
      scheduledTime: '14:30',
      doctorRoom: 'Phòng tiêm 05 - Tầng 2',
      status: 'confirmed',
    ),
  ];

  static const List<Appointment> _historyAppointments = [
    Appointment(
      id: 99,
      patientName: 'Bé Nguyễn Gia Hưng (2 tuổi)',
      vaccineName: 'Synflorix (Phế cầu 10)',
      doseInfo: 'Mũi 3',
      scheduledDate: '10/08/2026',
      scheduledTime: '10:00',
      doctorRoom: 'Phòng tiêm 01',
      status: 'completed',
      note: 'Không có phản ứng phụ bất thường sau 30 phút theo dõi.',
    ),
    Appointment(
      id: 98,
      patientName: 'Nguyễn Ngọc Quý',
      vaccineName: 'Engerix B (Viêm gan B)',
      doseInfo: 'Mũi 3',
      scheduledDate: '12/03/2026',
      scheduledTime: '08:30',
      doctorRoom: 'Phòng tiêm 03',
      status: 'completed',
    ),
  ];

  static final AftercareRecord _currentAftercare = AftercareRecord(
    id: 201,
    patientName: 'Bé Nguyễn Gia Hưng (2 tuổi)',
    vaccineName: 'Hexaxim 6 trong 1 (Mũi 4)',
    injectionTime: DateTime.now().subtract(const Duration(minutes: 18)),
    observationDurationMinutes: 30,
    status: 'observing',
    temperature: 36.8,
    doctorNotes:
        'Bé tỉnh táo, không quấy khóc, vết tiêm khô ráo, nhiệt độ bình thường.',
  );

  final List<VaccineReminder> _reminders = [
    const VaccineReminder(
      id: 301,
      patientName: 'Bé Nguyễn Gia Hưng (2 tuổi)',
      vaccineName: 'Phế cầu Synflorix',
      dose: 'Mũi nhắc 4',
      targetDate: '28/10/2026',
      daysRemaining: 7,
      channel: 'Thông báo App',
      isEnabled: true,
    ),
    const VaccineReminder(
      id: 302,
      patientName: 'Nguyễn Ngọc Quý',
      vaccineName: 'Vaxigrip Tetra (Cúm mùa)',
      dose: 'Mũi hàng năm',
      targetDate: '15/11/2026',
      daysRemaining: 25,
      channel: 'Zalo ZNS',
      isEnabled: true,
    ),
    const VaccineReminder(
      id: 303,
      patientName: 'Bé Nguyễn Gia Hưng (2 tuổi)',
      vaccineName: 'Thủy đậu Varilrix',
      dose: 'Mũi 2',
      targetDate: '10/12/2026',
      daysRemaining: 50,
      channel: 'SMS',
      isEnabled: false,
    ),
  ];

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 4, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  void _showReportSymptomModal(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => Container(
        padding: const EdgeInsets.all(20),
        decoration: const BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Ghi nhận phản ứng sau tiêm',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
                ),
                IconButton(
                  icon: const Icon(Icons.close_rounded),
                  onPressed: () => Navigator.pop(ctx),
                ),
              ],
            ),
            const SizedBox(height: 12),
            const Text(
              'Chọn các biểu hiện quan sát được sau khi tiêm để bác sĩ tư vấn:',
              style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
            ),
            const SizedBox(height: 14),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _buildSymptomChip('Biểu hiện mẫu'),
                _buildSymptomChip('Sưng đau tại vết tiêm'),
                _buildSymptomChip('Quấy khóc / Khó chịu'),
                _buildSymptomChip('Bỏ bú / Biếng ăn'),
                _buildSymptomChip('Phát ban nhẹ'),
              ],
            ),
            const SizedBox(height: 20),
            FilledButton(
              onPressed: () {
                Navigator.pop(ctx);
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(
                    content: Text(
                      'Mô phỏng; chưa gửi báo cáo tới API hoặc bác sĩ.',
                    ),
                    behavior: SnackBarBehavior.floating,
                  ),
                );
              },
              child: const Center(child: Text('Gửi báo cáo y khoa')),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSymptomChip(String label) {
    return FilterChip(
      label: Text(label, style: const TextStyle(fontSize: 12)),
      selected: false,
      onSelected: (_) {},
    );
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Integration Notice Banner (Matching requirement & maintaining test compatibility)
        Container(
          margin: const EdgeInsets.fromLTRB(16, 12, 16, 4),
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: AppColors.primarySoft,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: AppColors.primaryLight),
          ),
          child: const Row(
            children: [
              Icon(
                Icons.info_outline_rounded,
                size: 20,
                color: AppColors.primary,
              ),
              SizedBox(width: 10),
              Expanded(
                child: Text(
                  'Lịch hẹn sẽ có sau khi kết nối hồ sơ và đăng nhập. DỮ LIỆU MẪU hư cấu; chưa lưu hoặc gửi nhắc thật.',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppColors.primaryDark,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
            ],
          ),
        ),

        // 4 Tabs: Lịch hẹn, Lịch sử, Theo dõi sau tiêm, Nhắc lịch
        Container(
          margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          decoration: BoxDecoration(
            color: AppColors.surfaceSubtle,
            borderRadius: BorderRadius.circular(10),
          ),
          child: TabBar(
            controller: _tabController,
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            indicatorSize: TabBarIndicatorSize.tab,
            dividerColor: Colors.transparent,
            indicator: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(8),
              boxShadow: const [AppColors.subtleShadow],
            ),
            labelColor: AppColors.primaryDark,
            unselectedLabelColor: AppColors.textSecondary,
            labelStyle: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
            unselectedLabelStyle: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w500,
            ),
            tabs: const [
              Tab(text: 'Sắp tới (2)'),
              Tab(text: 'Lịch sử (2)'),
              Tab(text: 'Theo dõi sau tiêm'),
              Tab(text: 'Nhắc lịch (3)'),
            ],
          ),
        ),

        // Tab views
        Expanded(
          child: TabBarView(
            controller: _tabController,
            children: [
              // 1. Upcoming list
              ListView.separated(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                itemCount: _upcomingAppointments.length,
                separatorBuilder: (context, index) =>
                    const SizedBox(height: 12),
                itemBuilder: (context, index) {
                  return AppointmentCard(
                    appointment: _upcomingAppointments[index],
                  );
                },
              ),

              // 2. History list
              ListView.separated(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                itemCount: _historyAppointments.length,
                separatorBuilder: (context, index) =>
                    const SizedBox(height: 12),
                itemBuilder: (context, index) {
                  return AppointmentCard(
                    appointment: _historyAppointments[index],
                  );
                },
              ),

              // 3. Aftercare (Theo dõi sau tiêm 30p tại trung tâm & 48h tại nhà)
              ListView(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                children: [
                  AftercareCard(
                    record: _currentAftercare,
                    onReportSymptom: () => _showReportSymptomModal(context),
                  ),
                  const SizedBox(height: 16),

                  // Home care guidance card
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: const Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Icon(
                              Icons.health_and_safety_outlined,
                              size: 20,
                              color: AppColors.secondary,
                            ),
                            SizedBox(width: 8),
                            Text(
                              'Hướng dẫn (chưa tích hợp)',
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ],
                        ),
                        SizedBox(height: 10),
                        Text(
                          'Chờ bổ sung theo đề cương và nội dung chuyên môn được duyệt.',
                          style: TextStyle(
                            fontSize: 12,
                            height: 1.5,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),

              // 4. Reminders (Nhắc lịch tiêm)
              ListView.separated(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                itemCount: _reminders.length,
                separatorBuilder: (context, index) =>
                    const SizedBox(height: 12),
                itemBuilder: (context, index) {
                  final reminder = _reminders[index];
                  return ReminderCard(
                    reminder: reminder,
                    onToggle: (enabled) {
                      setState(() {
                        _reminders[index] = reminder.copyWith(
                          isEnabled: enabled,
                        );
                      });
                    },
                    onChangeChannel: (newChannel) {
                      setState(() {
                        _reminders[index] = reminder.copyWith(
                          channel: newChannel,
                        );
                      });
                    },
                  );
                },
              ),
            ],
          ),
        ),
      ],
    );
  }
}
