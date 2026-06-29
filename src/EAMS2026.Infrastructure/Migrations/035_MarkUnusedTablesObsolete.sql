-- ============================================================
-- Migration 035: 标记不使用的表为废弃（增加 _obsolete_ 前缀）
-- 说明：以下表由迁移文件创建，但没有任何 C# 代码查询它们：
--   1. hwatt_sync_logs   - 由 009_HwattSyncTables.sql 创建，代码中无引用
--   2. dingtalk_sync_logs - 由 014_DingTalkSyncTables.sql 创建，代码中无引用
-- 给这些表增加 _obsolete_ 前缀，方便后续确认清理。
-- ============================================================

-- ============================================================
-- 步骤 1：验证条件
-- ============================================================

DO $$
DECLARE
    hwatt_exists BOOLEAN;
    dingtalk_exists BOOLEAN;
    active_count INT;
BEGIN
    SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'hwatt_sync_logs' AND relkind = 'r') INTO hwatt_exists;
    SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'dingtalk_sync_logs' AND relkind = 'r') INTO dingtalk_exists;

    IF NOT hwatt_exists AND NOT dingtalk_exists THEN
        RAISE NOTICE 'hwatt_sync_logs 和 dingtalk_sync_logs 均不存在，无需操作';
        RETURN;
    END IF;

    -- 检查是否有活跃连接
    SELECT COUNT(*) INTO active_count
    FROM pg_stat_activity
    WHERE datname = current_database()
      AND state IN ('active', 'idle in transaction')
      AND pid <> pg_backend_pid();

    IF active_count > 0 THEN
        RAISE WARNING '存在 % 个活跃连接，建议在业务低峰期执行', active_count;
    END IF;

    RAISE NOTICE '开始标记废弃表...';
END $$;

-- ============================================================
-- 步骤 2：备份并重命名 hwatt_sync_logs
-- ============================================================

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'hwatt_sync_logs' AND relkind = 'r') THEN
        -- 备份数据
        CREATE TABLE IF NOT EXISTS hwatt_sync_logs_bak AS
        SELECT * FROM hwatt_sync_logs;

        RAISE NOTICE 'hwatt_sync_logs 已备份（% 条记录）',
            (SELECT COUNT(*) FROM hwatt_sync_logs);

        -- 重命名索引
        ALTER INDEX IF EXISTS idx_hwatt_sync_logs_type_time RENAME TO idx_obsolete_hwatt_sync_logs_type_time;

        -- 重命名表
        ALTER TABLE hwatt_sync_logs RENAME TO obsolete_hwatt_sync_logs;

        RAISE NOTICE 'hwatt_sync_logs → obsolete_hwatt_sync_logs';
    ELSE
        RAISE NOTICE 'hwatt_sync_logs 不存在，跳过';
    END IF;
END $$;

-- ============================================================
-- 步骤 3：备份并重命名 dingtalk_sync_logs
-- ============================================================

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'dingtalk_sync_logs' AND relkind = 'r') THEN
        -- 备份数据
        CREATE TABLE IF NOT EXISTS dingtalk_sync_logs_bak AS
        SELECT * FROM dingtalk_sync_logs;

        RAISE NOTICE 'dingtalk_sync_logs 已备份（% 条记录）',
            (SELECT COUNT(*) FROM dingtalk_sync_logs);

        -- 重命名索引
        ALTER INDEX IF EXISTS idx_dingtalk_sync_logs_type_time RENAME TO idx_obsolete_dingtalk_sync_logs_type_time;

        -- 重命名表
        ALTER TABLE dingtalk_sync_logs RENAME TO obsolete_dingtalk_sync_logs;

        RAISE NOTICE 'dingtalk_sync_logs → obsolete_dingtalk_sync_logs';
    ELSE
        RAISE NOTICE 'dingtalk_sync_logs 不存在，跳过';
    END IF;
END $$;

-- ============================================================
-- 步骤 4：更新表注释
-- ============================================================

COMMENT ON TABLE obsolete_hwatt_sync_logs IS '【废弃】HWATT数据同步日志表（由 Migration 035 标记为废弃，代码中已无引用）';
COMMENT ON TABLE obsolete_dingtalk_sync_logs IS '【废弃】钉钉数据同步日志表（由 Migration 035 标记为废弃，代码中已无引用）';

-- ============================================================
-- 步骤 5：验证
-- ============================================================

DO $$
DECLARE
    hwatt_old BOOLEAN;
    hwatt_new BOOLEAN;
    dingtalk_old BOOLEAN;
    dingtalk_new BOOLEAN;
BEGIN
    SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'hwatt_sync_logs' AND relkind = 'r') INTO hwatt_old;
    SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'obsolete_hwatt_sync_logs' AND relkind = 'r') INTO hwatt_new;
    SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'dingtalk_sync_logs' AND relkind = 'r') INTO dingtalk_old;
    SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'obsolete_dingtalk_sync_logs' AND relkind = 'r') INTO dingtalk_new;

    IF NOT hwatt_old AND hwatt_new THEN
        RAISE NOTICE '✅ hwatt_sync_logs → obsolete_hwatt_sync_logs';
    END IF;
    IF NOT dingtalk_old AND dingtalk_new THEN
        RAISE NOTICE '✅ dingtalk_sync_logs → obsolete_dingtalk_sync_logs';
    END IF;
END $$;