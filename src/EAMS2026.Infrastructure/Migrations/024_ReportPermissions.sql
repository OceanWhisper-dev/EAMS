-- ============================================================
-- 024_ReportPermissions.sql
-- 报表模块权限（菜单 + 按钮）
-- ============================================================

-- 如果 report 顶级菜单不存在则创建
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '报表管理', 'report', 'menu', NULL, '/main/reports', 'TrendCharts', 7, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'report' AND is_deleted = FALSE);

-- 报表查看按钮
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '报表查看', 'report:view', 'button', (SELECT id FROM permissions WHERE code = 'report' LIMIT 1), 1, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'report:view' AND is_deleted = FALSE);

-- 报表创建按钮
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '报表创建', 'report:create', 'button', (SELECT id FROM permissions WHERE code = 'report' LIMIT 1), 2, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'report:create' AND is_deleted = FALSE);

-- 报表编辑按钮
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '报表编辑', 'report:edit', 'button', (SELECT id FROM permissions WHERE code = 'report' LIMIT 1), 3, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'report:edit' AND is_deleted = FALSE);

-- 报表删除按钮
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '报表删除', 'report:delete', 'button', (SELECT id FROM permissions WHERE code = 'report' LIMIT 1), 4, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'report:delete' AND is_deleted = FALSE);

-- 报表导出按钮
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '报表导出', 'report:export', 'button', (SELECT id FROM permissions WHERE code = 'report' LIMIT 1), 5, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'report:export' AND is_deleted = FALSE);

-- 将以上所有权限授予超级管理员角色
INSERT INTO role_permissions (role_id, permission_id)
SELECT 1, p.id
FROM permissions p
WHERE p.code LIKE 'report%'
  AND p.is_deleted = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM role_permissions rp
    WHERE rp.role_id = 1 AND rp.permission_id = p.id
  );