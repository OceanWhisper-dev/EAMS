-- ============================================================
-- Migration 041: 修复权限数据结构（问题 1-4）
-- 1. 清理 test:* 测试权限
-- 2. 修复 type 字段错误值（0/1 → menu/button）
-- 3. 分离 ERP 子模块权限编码
-- 4. 修复 data-permission 编码不一致
-- ============================================================

-- ========== 问题 1: 清理测试权限 ==========
-- 软删除 test:* 权限及其关联角色权限
DELETE FROM sys_role_permissions
WHERE permission_id IN (SELECT id FROM sys_permissions WHERE code LIKE 'test:%' OR code = 'test:menu');
UPDATE sys_permissions SET is_deleted = TRUE
WHERE code LIKE 'test:%' OR code = 'test:menu';
-- 更新子权限的父级引用
UPDATE sys_permissions SET parent_id = NULL WHERE parent_id IN (SELECT id FROM sys_permissions WHERE code LIKE 'test:%' OR code = 'test:menu');

-- ========== 问题 2: 修复 type 字段 ==========
UPDATE sys_permissions SET type = 'menu'   WHERE id = 12 AND type = '0';  -- attendance 根菜单
UPDATE sys_permissions SET type = 'menu'   WHERE id = 24 AND type = '0';  -- attendance:employee-list
UPDATE sys_permissions SET type = 'button' WHERE id = 11 AND type = '1';  -- attendance:report:search
UPDATE sys_permissions SET type = 'menu'   WHERE id = 35 AND type = '1';  -- erp:datasource → erp-settings:datasource
UPDATE sys_permissions SET type = 'menu'   WHERE id = 36 AND type = '1';  -- erp:salesperson → erp-settings:salesperson
UPDATE sys_permissions SET type = 'button' WHERE id = 37 AND type = '1';  -- report:permission

-- ========== 问题 3: 分离 ERP 子模块权限编码 ==========
-- VouchModify 子模块
UPDATE sys_permissions SET code = 'vouch-modify:date'     WHERE id = 39;
UPDATE sys_permissions SET code = 'vouch-modify:order'    WHERE id = 40;
UPDATE sys_permissions SET code = 'vouch-modify:dispatch' WHERE id = 41;
UPDATE sys_permissions SET code = 'vouch-modify:template' WHERE id = 42;
UPDATE sys_permissions SET code = 'vouch-modify:log'      WHERE id = 43;

-- ErpSettings 子模块
UPDATE sys_permissions SET code = 'erp-settings:datasource'   WHERE id = 35;
UPDATE sys_permissions SET code = 'erp-settings:salesperson'  WHERE id = 36;

-- ========== 问题 4: 修复 data-permission 编码 ==========
UPDATE sys_permissions SET code = 'data-permission:config' WHERE id = 27;