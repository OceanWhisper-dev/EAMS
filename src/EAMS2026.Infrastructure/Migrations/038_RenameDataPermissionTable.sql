-- ============================================================
-- Migration 038: data_permission_rules → sys_data_permission_rules
-- 说明：该表属于系统管理模块，按规范增加 sys_ 前缀
-- ============================================================

-- 1. 重命名序列
ALTER SEQUENCE IF EXISTS data_permission_rules_id_seq RENAME TO sys_data_permission_rules_id_seq;

-- 2. 重命名主键索引
ALTER INDEX IF EXISTS data_permission_rules_pkey RENAME TO sys_data_permission_rules_pkey;

-- 3. 重命名唯一索引
ALTER INDEX IF EXISTS idx_data_permission_rules_unique RENAME TO idx_sys_data_permission_rules_unique;

-- 4. 删除外键约束（需要先删除才能重命名表）
ALTER TABLE IF EXISTS data_permission_rules DROP CONSTRAINT IF EXISTS data_permission_rules_role_id_fkey;

-- 5. 重命名表
ALTER TABLE IF EXISTS data_permission_rules RENAME TO sys_data_permission_rules;

-- 6. 重新创建外键约束
ALTER TABLE sys_data_permission_rules
    ADD CONSTRAINT fk_sys_data_permission_rules_role
    FOREIGN KEY (role_id) REFERENCES sys_roles(id);

-- 7. 添加表注释
COMMENT ON TABLE sys_data_permission_rules IS '数据权限规则表（由 Migration 038 重命名自 data_permission_rules）';