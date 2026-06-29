-- ============================================================
-- 数据权限配置页面权限（系统管理子菜单）
-- ============================================================

INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '数据权限配置', 'attendance:data-permission', 'menu', NULL, '/attendance/data-permission', 'Setting', 1, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:data-permission' AND is_deleted = FALSE);