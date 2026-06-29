-- ============================================================
-- Migration 028: 业务员映射表添加类型字段
-- 说明：区分业务员（受限，只能看自己数据）和主管（不受限，可看全部）
-- type = 'salesperson' → 业务员，数据受限
-- type = 'supervisor'  → 主管，数据不受限
-- 已有数据默认为 salesperson（保持向下兼容）
-- ============================================================

ALTER TABLE erp_rpt_salesperson_map ADD COLUMN IF NOT EXISTS type VARCHAR(20) NOT NULL DEFAULT 'salesperson';
COMMENT ON COLUMN erp_rpt_salesperson_map.type IS '类型：salesperson=业务员（受限），supervisor=主管（不受限）';