-- ============================================================
-- 测试权限数据（基础框架权限测试用）
-- 编码前缀 test: 便于后续清理
-- 注意：不要自动清理，等待用户手动通知后再清理
-- 清理命令: DELETE FROM role_permissions WHERE permission_id IN (SELECT id FROM permissions WHERE code LIKE 'test:%');
--           DELETE FROM permissions WHERE code LIKE 'test:%';
-- ============================================================

-- 1. 新增测试菜单（顶级菜单）
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
VALUES ('新增测试菜单', 'test:menu', 'menu', NULL, '/test', 'Tools', 999, FALSE, NOW(), 1, NOW(), 1);

-- 2. 新增测试菜单 - 权限1子菜单（子菜单）
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
VALUES ('新增测试菜单 - 权限1子菜单', 'test:permission1', 'menu', (SELECT id FROM permissions WHERE code = 'test:menu'), '/test/permission1', NULL, 1, FALSE, NOW(), 1, NOW(), 1);

-- 3. 新增测试菜单 - 权限2子菜单（子菜单）
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
VALUES ('新增测试菜单 - 权限2子菜单', 'test:permission2', 'menu', (SELECT id FROM permissions WHERE code = 'test:menu'), '/test/permission2', NULL, 2, FALSE, NOW(), 1, NOW(), 1);

-- 4. 权限1子菜单 - 功能11按钮
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
VALUES ('权限1子菜单 - 功能11', 'test:func11', 'button', (SELECT id FROM permissions WHERE code = 'test:permission1'), NULL, NULL, 1, FALSE, NOW(), 1, NOW(), 1);

-- 5. 权限1子菜单 - 功能12按钮
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
VALUES ('权限1子菜单 - 功能12', 'test:func12', 'button', (SELECT id FROM permissions WHERE code = 'test:permission1'), NULL, NULL, 2, FALSE, NOW(), 1, NOW(), 1);

-- 6. 权限2子菜单 - 功能21按钮
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
VALUES ('权限2子菜单 - 功能21', 'test:func21', 'button', (SELECT id FROM permissions WHERE code = 'test:permission2'), NULL, NULL, 1, FALSE, NOW(), 1, NOW(), 1);

-- 7. 权限2子菜单 - 功能22按钮
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
VALUES ('权限2子菜单 - 功能22', 'test:func22', 'button', (SELECT id FROM permissions WHERE code = 'test:permission2'), NULL, NULL, 2, FALSE, NOW(), 1, NOW(), 1);

-- 8. 权限2子菜单 - 功能23按钮
INSERT INTO permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
VALUES ('权限2子菜单 - 功能23', 'test:func23', 'button', (SELECT id FROM permissions WHERE code = 'test:permission2'), NULL, NULL, 3, FALSE, NOW(), 1, NOW(), 1);