import 'package:flutter/material.dart';

/// Healthcare & Vaccination design system color palette
abstract final class AppColors {
  // Brand Primary & Accent
  static const Color primary = Color(0xFF0284C7); // Clinical Sky/Cerulean
  static const Color primaryDark = Color(0xFF0369A1);
  static const Color primaryLight = Color(0xFFE0F2FE);
  static const Color primarySoft = Color(0xFFF0F9FF);

  static const Color secondary = Color(0xFF0D9488); // Medical Teal
  static const Color secondaryLight = Color(0xFFCCFBF1);

  // Background & Surfaces
  static const Color background = Color(0xFFF8FAFC); // Clean Slate-50
  static const Color surface = Color(0xFFFFFFFF);
  static const Color surfaceSubtle = Color(0xFFF1F5F9); // Slate-100
  static const Color surfaceElevated = Color(0xFFFFFFFF);

  // Borders & Dividers
  static const Color border = Color(0xFFE2E8F0); // Slate-200
  static const Color borderFocus = Color(0xFF38BDF8); // Sky-400

  // Text colors
  static const Color textPrimary = Color(0xFF0F172A); // Slate-900
  static const Color textSecondary = Color(0xFF475569); // Slate-600
  static const Color textMuted = Color(0xFF94A3B8); // Slate-400
  static const Color textWhite = Color(0xFFFFFFFF);

  // Status & Semantics
  static const Color success = Color(0xFF10B981);
  static const Color successBg = Color(0xFFECFDF5);
  static const Color warning = Color(0xFFF59E0B);
  static const Color warningBg = Color(0xFFFFFBEB);
  static const Color error = Color(0xFFEF4444);
  static const Color errorBg = Color(0xFFFEF2F2);
  static const Color info = Color(0xFF3B82F6);
  static const Color infoBg = Color(0xFFEFF6FF);

  // Shadows
  static const BoxShadow cardShadow = BoxShadow(
    color: Color(0x0A0F172A),
    blurRadius: 16,
    offset: Offset(0, 4),
  );

  static const BoxShadow subtleShadow = BoxShadow(
    color: Color(0x050F172A),
    blurRadius: 8,
    offset: Offset(0, 2),
  );
}
