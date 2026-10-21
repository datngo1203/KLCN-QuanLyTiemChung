import 'package:flutter/material.dart';
import '../../../../core/constants/app_constants.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/widgets/app_search_field.dart';
import '../../../../core/widgets/empty_state_view.dart';
import '../../../../core/widgets/error_view.dart';
import '../../domain/models/vaccine.dart';
import '../../domain/repositories/catalog_repository.dart';
import '../widgets/category_filter_bar.dart';
import '../widgets/vaccine_card.dart';

class CatalogScreen extends StatefulWidget {
  const CatalogScreen({super.key, required this.repository});

  final CatalogRepository repository;

  @override
  State<CatalogScreen> createState() => _CatalogScreenState();
}

class _CatalogScreenState extends State<CatalogScreen> {
  late Future<List<Vaccine>> _future;
  String _searchQuery = '';
  int _categoryIndex = 0;

  @override
  void initState() {
    super.initState();
    _loadVaccines();
  }

  void _loadVaccines() {
    setState(() {
      _future = widget.repository.getVaccines(
        search: _searchQuery.isNotEmpty ? _searchQuery : null,
      );
    });
  }

  Future<void> _handleRefresh() async {
    _loadVaccines();
    await _future;
  }

  void _onSearchChanged(String query) {
    setState(() {
      _searchQuery = query;
      _loadVaccines();
    });
  }

  List<Vaccine> _applyCategoryFilter(List<Vaccine> items) {
    if (_categoryIndex == 0) return items;
    final catName = AppConstants.catalogCategories[_categoryIndex].toLowerCase();

    return items.where((v) {
      final name = v.name.toLowerCase();
      final target = (v.targetGroup ?? '').toLowerCase();
      if (catName.contains('trẻ em')) {
        return name.contains('trẻ') || target.contains('trẻ') || name.contains('6 trong 1');
      }
      if (catName.contains('người lớn')) {
        return name.contains('lớn') || target.contains('lớn') || name.contains('hpv') || name.contains('cúm');
      }
      if (catName.contains('thai')) {
        return name.contains('uốn ván') || name.contains('bạch hầu') || target.contains('thai');
      }
      if (catName.contains('gói')) {
        return name.contains('gói');
      }
      return true;
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Top search & filter area
        Container(
          color: AppColors.surface,
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              AppSearchField(
                hintText: 'Tìm vắc xin, bệnh phòng ngừa...',
                onChanged: _onSearchChanged,
              ),
              const SizedBox(height: 12),
              CategoryFilterBar(
                categories: AppConstants.catalogCategories,
                selectedIndex: _categoryIndex,
                onSelect: (index) {
                  setState(() => _categoryIndex = index);
                },
              ),
            ],
          ),
        ),
        const Divider(),

        // Vaccine items list
        Expanded(
          child: FutureBuilder<List<Vaccine>>(
            future: _future,
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) {
                return const Center(
                  child: CircularProgressIndicator(
                    strokeWidth: 2.5,
                    color: AppColors.primary,
                  ),
                );
              }

              if (snapshot.hasError) {
                final error = snapshot.error;
                return ErrorView(
                  message: error?.toString() ?? 'Chưa tải được danh mục. Kiểm tra kết nối API.',
                  onRetry: _loadVaccines,
                );
              }

              final rawItems = snapshot.data ?? [];
              final filteredItems = _applyCategoryFilter(rawItems);

              if (filteredItems.isEmpty) {
                return EmptyStateView(
                  icon: Icons.search_off_rounded,
                  title: 'Không tìm thấy vắc xin phù hợp',
                  description: _searchQuery.isNotEmpty
                      ? 'Không có kết quả cho từ khóa "$_searchQuery". Bạn hãy thử tìm kiếm tên bệnh hoặc nhà sản xuất.'
                      : 'Chưa có vắc xin trong danh mục này.',
                  actionLabel: _searchQuery.isNotEmpty ? 'Xóa tìm kiếm' : 'Tải lại',
                  onAction: () {
                    if (_searchQuery.isNotEmpty) {
                      _onSearchChanged('');
                    } else {
                      _loadVaccines();
                    }
                  },
                );
              }

              return RefreshIndicator(
                color: AppColors.primary,
                backgroundColor: AppColors.surface,
                onRefresh: _handleRefresh,
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
                  itemCount: filteredItems.length + 1,
                  separatorBuilder: (context, index) {
                    if (index == 0) return const SizedBox(height: 12);
                    return const SizedBox(height: 12);
                  },
                  itemBuilder: (context, index) {
                    if (index == 0) {
                      return Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            'Tìm thấy ${filteredItems.length} vắc xin',
                            style: const TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          const Text(
                            'Đảm bảo tiêu chuẩn GSP',
                            style: TextStyle(
                              fontSize: 12,
                              color: AppColors.secondary,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                        ],
                      );
                    }
                    final vaccine = filteredItems[index - 1];
                    return VaccineCard(
                      key: ValueKey(vaccine.id),
                      vaccine: vaccine,
                    );
                  },
                ),
              );
            },
          ),
        ),
      ],
    );
  }
}
