-- ============================================================
-- 026_ReportAdditionalPermissions.sql
-- 报表模块补充权限（数据源管理、业务员映射、报表权限）
-- ============================================================

-- 数据源管理按钮
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '数据源管理', 'report:datasource', 'button', (SELECT id FROM permissions WHERE code = 'report' AND is_deleted = FALSE LIMIT 1), 6, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'report:datasource' AND is_deleted = FALSE);

-- 业务员映射按钮
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '业务员映射', 'report:salesperson', 'button', (SELECT id FROM permissions WHERE code = 'report' AND is_deleted = FALSE LIMIT 1), 7, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'report:salesperson' AND is_deleted = FALSE);

-- 报表权限管理按钮
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '报表权限', 'report:permission', 'button', (SELECT id FROM permissions WHERE code = 'report' AND is_deleted = FALSE LIMIT 1), 8, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'report:permission' AND is_deleted = FALSE);

-- 将以上所有新权限授予超级管理员角色
INSERT INTO role_permissions (role_id, permission_id)
SELECT 1, p.id
FROM permissions p
WHERE p.code IN ('report:datasource', 'report:salesperson', 'report:permission')
  AND p.is_deleted = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM role_permissions rp
    WHERE rp.role_id = 1 AND rp.permission_id = p.id
  );