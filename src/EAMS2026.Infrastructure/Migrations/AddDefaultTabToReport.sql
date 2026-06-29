-- 添加 default_tab 字段到 erp_rpt_report 表
-- 用于控制报表查看器默认打开数据表还是透视表
ALTER TABLE erp_rpt_report ADD COLUMN IF NOT EXISTS default_tab VARCHAR(20) NOT NULL DEFAULT 'table';
