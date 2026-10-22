import 'package:flutter/material.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/widgets/app_badge.dart';
import '../../domain/models/vaccine_reminder.dart';

class ReminderCard extends StatelessWidget {
  const ReminderCard({
    super.key,
    required this.reminder,
    required this.onToggle,
    required this.onChangeChannel,
  });

  final VaccineReminder reminder;
  final ValueChanged<bool> onToggle;
  final ValueChanged<String> onChangeChannel;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
        boxShadow: const [AppColors.cardShadow],
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Top: Patient + Toggle
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    const Icon(Icons.notifications_active_outlined, size: 18, color: AppColors.primary),
                    const SizedBox(width: 6),
                    Text(
                      reminder.patientName,
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                  ],
                ),
                Switch.adaptive(
                  value: reminder.isEnabled,
                  onChanged: onToggle,
                  activeTrackColor: AppColors.primary,
                ),
              ],
            ),
            const SizedBox(height: 6),

            // Vaccine info + Dose badge
            Row(
              children: [
                Expanded(
                  child: Text(
                    reminder.vaccineName,
                    style: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                AppBadge.info(text: reminder.dose),
              ],
            ),
            const SizedBox(height: 10),

            // Date & countdown box
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              decoration: BoxDecoration(
                color: AppColors.surfaceSubtle,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.event_available_outlined, size: 16, color: AppColors.textSecondary),
                      const SizedBox(width: 6),
                      Text(
                        'Ngày tiêm dự kiến: ${reminder.targetDate}',
                        style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
                      ),
                    ],
                  ),
                  Text(
                    'Còn ${reminder.daysRemaining} ngày',
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                      color: AppColors.primaryDark,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),

            // Channel selector
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Kênh nhận nhắc hẹn:',
                  style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
                ),
                DropdownButton<String>(
                  value: reminder.channel,
                  underline: const SizedBox.shrink(),
                  isDense: true,
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: AppColors.primaryDark,
                  ),
                  items: const [
                    DropdownMenuItem(value: 'Thông báo App', child: Text('Thông báo App')),
                    DropdownMenuItem(value: 'SMS', child: Text('Tin nhắn SMS')),
                    DropdownMenuItem(value: 'Zalo ZNS', child: Text('Zalo ZNS')),
                  ],
                  onChanged: (val) {
                    if (val != null) onChangeChannel(val);
                  },
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
