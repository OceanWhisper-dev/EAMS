-- ============================================
-- 033_RenameVouchModifyLogs.sql
-- 将 vouch_modify_logs 表重命名为 erp_vouch_modify_logs
-- 以符合 ERP 模块 erp_ 前缀规范
-- ============================================

-- 仅当旧表存在且新表不存在时执行重命名
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'vouch_modify_logs' AND relkind = 'r')
       AND NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'erp_vouch_modify_logs' AND relkind = 'r') THEN
        ALTER TABLE vouch_modify_logs RENAME TO erp_vouch_modify_logs;

        -- 重命名索引
        ALTER INDEX IF EXISTS idx_vouch_modify_logs_vouch_type RENAME TO idx_erp_vouch_modify_logs_vouch_type;
        ALTER INDEX IF EXISTS idx_vouch_modify_logs_operator_id RENAME TO idx_erp_vouch_modify_logs_operator_id;
        ALTER INDEX IF EXISTS idx_vouch_modify_logs_operate_at RENAME TO idx_erp_vouch_modify_logs_operate_at;
    END IF;
END $$;