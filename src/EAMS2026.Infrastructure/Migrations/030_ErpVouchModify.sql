-- ============================================
-- 030_ERP单据修改模块
-- 创建 U8 ERP 单据修改功能所需的PostgreSQL表
-- ============================================

-- 1. 修改日志表
CREATE TABLE IF NOT EXISTS erp_vouch_modify_logs (
    id BIGSERIAL PRIMARY KEY,
    vouch_type VARCHAR(20) NOT NULL CHECK (vouch_type IN ('order', 'dispatch')),
    vouch_id BIGINT NOT NULL,
    vouch_code VARCHAR(100) NOT NULL,
    field_name VARCHAR(50) NOT NULL,
    old_value TEXT,
    new_value TEXT,
    operator_id BIGINT NOT NULL,
    operator_name VARCHAR(100) NOT NULL,
    operate_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status VARCHAR(20) NOT NULL DEFAULT 'SUCCESS' CHECK (status IN ('SUCCESS', 'FAILED')),
    error_msg TEXT
);

COMMENT ON TABLE erp_vouch_modify_logs IS 'ERP单据修改操作日志';
COMMENT ON COLUMN erp_vouch_modify_logs.vouch_id IS '单据ID (soid / dlid)';
COMMENT ON COLUMN erp_vouch_modify_logs.vouch_code IS '单据编号 (cSOCode / cDLCode)';
COMMENT ON COLUMN erp_vouch_modify_logs.field_name IS '修改的字段名';
COMMENT ON COLUMN erp_vouch_modify_logs.status IS '操作状态: SUCCESS=成功, FAILED=失败';

-- 索引
CREATE INDEX IF NOT EXISTS idx_erp_vouch_modify_logs_vouch_type ON erp_vouch_modify_logs(vouch_type);
CREATE INDEX IF NOT EXISTS idx_erp_vouch_modify_logs_operator_id ON erp_vouch_modify_logs(operator_id);
CREATE INDEX IF NOT EXISTS idx_erp_vouch_modify_logs_operate_at ON erp_vouch_modify_logs(operate_at DESC);