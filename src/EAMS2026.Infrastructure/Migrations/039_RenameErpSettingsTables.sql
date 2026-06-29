-- ============================================================
-- Migration 039: erp_rpt_datasource/salesperson_map → erp_settings_*
-- 说明：这两张表属于 ErpSettings 模块（ERP 设置），
--       而非 Report 模块。按模块规范更正前缀。
-- ============================================================

-- ========== 1. erp_rpt_datasource → erp_settings_datasource ==========

-- 重命名序列
ALTER SEQUENCE IF EXISTS rpt_datasource_id_seq RENAME TO erp_settings_datasource_id_seq;

-- 删除外键约束（如果有引用本表的 FK，先删除）
-- (erp_rpt_datasource 没有 FK 引用其他表)

-- 重命名索引
ALTER INDEX IF EXISTS erp_rpt_datasource_pkey       RENAME TO erp_settings_datasource_pkey;
ALTER INDEX IF EXISTS idx_erp_rpt_datasource_name    RENAME TO idx_erp_settings_datasource_name;
ALTER INDEX IF EXISTS idx_erp_rpt_datasource_enabled RENAME TO idx_erp_settings_datasource_enabled;

-- 重命名表
ALTER TABLE IF EXISTS erp_rpt_datasource RENAME TO erp_settings_datasource;

-- 更新注释
COMMENT ON TABLE erp_settings_datasource IS 'ERP设置-数据源配置（由 Migration 039 重命名自 erp_rpt_datasource）';

-- ========== 2. erp_rpt_salesperson_map → erp_settings_salesperson_map ==========

-- 删除外键约束
ALTER TABLE IF EXISTS erp_rpt_salesperson_map DROP CONSTRAINT IF EXISTS rpt_salesperson_map_employee_id_fkey;

-- 重命名索引
ALTER INDEX IF EXISTS erp_rpt_salesperson_map_pkey     RENAME TO erp_settings_salesperson_map_pkey;
ALTER INDEX IF EXISTS idx_erp_rpt_salesperson_map_code RENAME TO idx_erp_settings_salesperson_map_code;

-- 重命名表
ALTER TABLE IF EXISTS erp_rpt_salesperson_map RENAME TO erp_settings_salesperson_map;

-- 重建外键约束
ALTER TABLE erp_settings_salesperson_map
    ADD CONSTRAINT fk_erp_settings_salesperson_map_employee
    FOREIGN KEY (employee_id) REFERENCES sys_employees(id) ON DELETE CASCADE;

-- 更新注释
COMMENT ON TABLE erp_settings_salesperson_map IS 'ERP设置-员工业务员映射表（由 Migration 039 重命名自 erp_rpt_salesperson_map）';
COMMENT ON COLUMN erp_settings_salesperson_map.employee_id IS '系统员工ID';
COMMENT ON COLUMN erp_settings_salesperson_map.salesperson_code IS 'ERP业务员编码（对应U8 Person.cPersonCode）';
COMMENT ON COLUMN erp_settings_salesperson_map.salesperson_name IS '业务员名称（冗余，方便查看）';
COMMENT ON COLUMN erp_settings_salesperson_map.type IS '类型：salesperson=业务员（受限），supervisor=主管（不受限）';