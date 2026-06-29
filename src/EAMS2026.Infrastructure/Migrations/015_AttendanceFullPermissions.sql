-- ============================================================
-- 015_AttendanceFullPermissions.sql
-- 补全考勤管理模块的页面和按钮权限
-- ============================================================

-- 如果 attendance 顶级菜单不存在则创建
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '考勤管理', 'attendance', 'menu', NULL, NULL, 'Calendar', 6, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance' AND is_deleted = FALSE);

-- 创建子页面菜单权限（不存在则新增）
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '考勤报表', 'attendance:report', 'menu', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), '/attendance/report', 'Calendar', 1, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:report' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '考勤类型', 'attendance:day-type', 'menu', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), '/attendance/day-type', 'CollectionTag', 2, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:day-type' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '排班类别', 'attendance:scheme-class', 'menu', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), '/attendance/scheme-class', 'Tickets', 3, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:scheme-class' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '计划标准时间', 'attendance:plan-time', 'menu', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), '/attendance/plan-time', 'Clock', 4, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:plan-time' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '节假日管理', 'attendance:holiday', 'menu', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), '/attendance/holiday', 'Sunny', 5, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:holiday' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '费用计算规则', 'attendance:fee-calculator', 'menu', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), '/attendance/fee-calculator', 'Coin', 6, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:fee-calculator' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '考勤员工', 'attendance:employee-list', 'menu', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), '/attendance/attendance-employees', 'User', 7, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:employee-list' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '员工关联班次', 'attendance:employee-ref-class', 'menu', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), '/attendance/employee-ref-class', 'Link', 8, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:employee-ref-class' AND is_deleted = FALSE);

-- 补充缺失的按钮权限（存在则跳过）
INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '导入所有员工考勤', 'attendance:import-all', 'button', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), 1, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:import-all' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '导入考勤', 'attendance:import', 'button', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), 2, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:import' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '打印报表', 'attendance:print', 'button', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), 3, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:print' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '导出报表', 'attendance:export', 'button', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), 4, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:export' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '同步考勤员工', 'attendance:sync-employees', 'button', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), 5, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:sync-employees' AND is_deleted = FALSE);

INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
SELECT '同步打卡记录', 'attendance:sync-card-records', 'button', (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1), 6, FALSE, NOW(), 1, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'attendance:sync-card-records' AND is_deleted = FALSE);

-- 将以上所有权限授予超级管理员角色
INSERT INTO role_permissions (role_id, permission_id)
SELECT 1, p.id
FROM permissions p
WHERE p.code LIKE 'attendance:%'
  AND p.is_deleted = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM role_permissions rp
    WHERE rp.role_id = 1 AND rp.permission_id = p.id
  );