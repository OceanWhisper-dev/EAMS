-- ============================================================
-- EAMS2026 操作日志表 DDL
-- ============================================================

CREATE TABLE IF NOT EXISTS sys_operation_logs (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    user_name VARCHAR(100) NOT NULL,
    module VARCHAR(100) NOT NULL,
    operation VARCHAR(100) NOT NULL,
    description TEXT,
    old_values TEXT,
    new_values TEXT,
    ip_address VARCHAR(50) NOT NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by BIGINT NOT NULL DEFAULT 0,
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by BIGINT NOT NULL DEFAULT 0
);

CREATE INDEX idx_sys_operation_logs_user_id ON sys_operation_logs(user_id);
CREATE INDEX idx_sys_operation_logs_module ON sys_operation_logs(module);
CREATE INDEX idx_sys_operation_logs_created_at ON sys_operation_logs(created_at DESC);

COMMENT ON TABLE sys_operation_logs IS '操作日志表';
COMMENT ON COLUMN sys_operation_logs.user_id IS '操作用户ID';
COMMENT ON COLUMN sys_operation_logs.user_name IS '操作用户名';
COMMENT ON COLUMN sys_operation_logs.module IS '操作模块';
COMMENT ON COLUMN sys_operation_logs.operation IS '操作类型';
COMMENT ON COLUMN sys_operation_logs.description IS '操作描述';
COMMENT ON COLUMN sys_operation_logs.old_values IS '修改前值(JSON)';
COMMENT ON COLUMN sys_operation_logs.new_values IS '修改后值(JSON)';
COMMENT ON COLUMN sys_operation_logs.ip_address IS '操作IP地址';