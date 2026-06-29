-- ============================================================
-- 031_ErpVouchModifyPermissions.sql
-- ERP单据修改模块权限（菜单 + 页面按钮）
-- ============================================================

-- 1. erp 顶级菜单（不存在则创建）
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT 'ERP单据修改', 'erp', 'menu', NULL, NULL, 'Document', 8, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'erp' AND is_deleted = FALSE);

-- 2. 发货日期修改
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '发货日期修改', 'erp:vouch-date', 'button', (SELECT id FROM permissions WHERE code = 'erp' AND is_deleted = FALSE LIMIT 1), 1, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'erp:vouch-date' AND is_deleted = FALSE);

-- 3. 订单客户修改
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '订单客户修改', 'erp:vouch-order', 'button', (SELECT id FROM permissions WHERE code = 'erp' AND is_deleted = FALSE LIMIT 1), 2, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'erp:vouch-order' AND is_deleted = FALSE);

-- 4. 发货单客户修改
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '发货单客户修改', 'erp:vouch-dispatch', 'button', (SELECT id FROM permissions WHERE code = 'erp' AND is_deleted = FALSE LIMIT 1), 3, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'erp:vouch-dispatch' AND is_deleted = FALSE);

-- 5. 修改日志
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '修改日志', 'erp:vouch-log', 'button', (SELECT id FROM permissions WHERE code = 'erp' AND is_deleted = FALSE LIMIT 1), 4, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'erp:vouch-log' AND is_deleted = FALSE);

-- 7. 将以上所有权限授予超级管理员角色（角色ID=1）
INSERT INTO role_permissions (role_id, permission_id)
SELECT 1, p.id
FROM permissions p
WHERE p.code LIKE 'erp%'
  AND p.is_deleted = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM role_permissions rp
    WHERE rp.role_id = 1 AND rp.permission_id = p.id
  );