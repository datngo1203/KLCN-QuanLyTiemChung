import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

class AppBadge extends StatelessWidget {
  const AppBadge({
    super.key,
    required this.text,
    this.backgroundColor = AppColors.primarySoft,
    this.textColor = AppColors.primaryDark,
    this.icon,
    this.fontSize = 11,
    this.padding = const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
  });

  final String text;
  final Color backgroundColor;
  final Color textColor;
  final IconData? icon;
  final double fontSize;
  final EdgeInsetsGeometry padding;

  factory AppBadge.success({
    Key? key,
    required String text,
    IconData? icon,
  }) => AppBadge(
    key: key,
    text: text,
    backgroundColor: AppColors.successBg,
    textColor: AppColors.success,
    icon: icon,
  );

  factory AppBadge.warning({
    Key? key,
    required String text,
    IconData? icon,
  }) => AppBadge(
    key: key,
    text: text,
    backgroundColor: AppColors.warningBg,
    textColor: AppColors.warning,
    icon: icon,
  );

  factory AppBadge.info({
    Key? key,
    required String text,
    IconData? icon,
  }) => AppBadge(
    key: key,
    text: text,
    backgroundColor: AppColors.infoBg,
    textColor: AppColors.info,
    icon: icon,
  );

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: padding,
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: fontSize + 1, color: textColor),
            const SizedBox(width: 4),
          ],
          Text(
            text,
            style: TextStyle(
              color: textColor,
              fontSize: fontSize,
              fontWeight: FontWeight.w600,
              letterSpacing: 0.1,
            ),
          ),
        ],
      ),
    );
  }
}
