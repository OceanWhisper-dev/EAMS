-- ============================================================
-- 027_ReportDesignerPermission.sql
-- 报表设计权限（用于控制侧边栏「报表设计」菜单项可见性）
-- ============================================================

-- 报表设计按钮
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '报表设计', 'report:designer', 'button', (SELECT id FROM permissions WHERE code = 'report' AND is_deleted = FALSE LIMIT 1), 9, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'report:designer' AND is_deleted = FALSE);

-- 将报表设计权限授予超级管理员角色
INSERT INTO role_permissions (role_id, permission_id)
SELECT 1, p.id
FROM permissions p
WHERE p.code = 'report:designer'
  AND p.is_deleted = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM role_permissions rp
    WHERE rp.role_id = 1 AND rp.permission_id = p.id
  );