-- ============================================================
-- Migration 034: 统一将所有 rpt_ 表重命名为 erp_rpt_ 前缀
-- 说明：备份版本 v0.3.260613 中所有报表相关的迁移文件(022-029)
--       均使用 rpt_ 前缀创建表，后迁移文件已更新为 erp_rpt_ 前缀，
--       但存量数据库仍为旧名 rpt_，导致代码查询时报错
--       "relation "erp_rpt_xxx" does not exist"。
-- 此脚本统一重命名所有存量表。
-- ============================================================

-- ============================================================
-- 步骤 1：验证条件 - 检查是否存在旧表且新表名不冲突
-- ============================================================

DO $$
DECLARE
    tbl RECORD;
    old_name TEXT;
    new_name TEXT;
    has_old BOOLEAN := FALSE;
    conflict BOOLEAN := FALSE;
    active_count INT;
BEGIN
    -- 检查活跃连接
    SELECT COUNT(*) INTO active_count
    FROM pg_stat_activity
    WHERE datname = current_database()
      AND state IN ('active', 'idle in transaction')
      AND query ILIKE '%rpt_%'
      AND pid <> pg_backend_pid();

    IF active_count > 0 THEN
        RAISE WARNING '发现 % 个活跃连接/事务可能正在访问 rpt_ 表，建议在业务低峰期执行', active_count;
    END IF;

    -- 检查需要重命名的 14 个表
    FOR tbl IN
        SELECT unnest(ARRAY[
            'rpt_category', 'rpt_report', 'rpt_field', 'rpt_filter',
            'rpt_sort', 'rpt_chart', 'rpt_permission', 'rpt_field_permission',
            'rpt_bookmark', 'rpt_execution_log', 'rpt_datasource',
            'rpt_salesperson_map', 'rpt_pivot_view', 'rpt_pivot_view_share'
        ]) AS old_name
    LOOP
        IF EXISTS (SELECT 1 FROM pg_class WHERE relname = tbl.old_name AND relkind = 'r') THEN
            has_old := TRUE;
            new_name := 'erp_' || tbl.old_name;
            IF EXISTS (SELECT 1 FROM pg_class WHERE relname = new_name AND relkind = 'r') THEN
                RAISE WARNING '冲突：表 % 已存在，跳过 % 的重命名', new_name, tbl.old_name;
                conflict := TRUE;
            END IF;
        END IF;
    END LOOP;

    IF NOT has_old THEN
        RAISE EXCEPTION '未找到任何 rpt_ 前缀的表，无需重命名';
    END IF;

    IF conflict THEN
        RAISE EXCEPTION '存在表名冲突，请手动处理';
    END IF;

    RAISE NOTICE '条件检查通过，开始重命名';
END $$;

-- ============================================================
-- 步骤 2：自动备份所有 rpt_ 表
-- ============================================================

DO $$
DECLARE
    tbl RECORD;
    bak_name TEXT;
    bak_exists BOOLEAN;
    rec_count INT;
BEGIN
    FOR tbl IN
        SELECT unnest(ARRAY[
            'rpt_category', 'rpt_report', 'rpt_field', 'rpt_filter',
            'rpt_sort', 'rpt_chart', 'rpt_permission', 'rpt_field_permission',
            'rpt_bookmark', 'rpt_execution_log', 'rpt_datasource',
            'rpt_salesperson_map', 'rpt_pivot_view', 'rpt_pivot_view_share'
        ]) AS old_name
    LOOP
        IF EXISTS (SELECT 1 FROM pg_class WHERE relname = tbl.old_name AND relkind = 'r') THEN
            bak_name := tbl.old_name || '_bak_' || TO_CHAR(NOW(), 'YYYYMMDD');

            SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = bak_name AND relkind = 'r') INTO bak_exists;

            IF NOT bak_exists THEN
                EXECUTE FORMAT('CREATE TABLE %I AS SELECT * FROM %I', bak_name, tbl.old_name);
                EXECUTE FORMAT('SELECT COUNT(*) FROM %I', bak_name) INTO rec_count;
                RAISE NOTICE '备份 % → %（% 条记录）', tbl.old_name, bak_name, rec_count;
            ELSE
                RAISE NOTICE '备份 % 已存在，跳过', bak_name;
            END IF;
        END IF;
    END LOOP;
END $$;

-- ============================================================
-- 步骤 3：统一重命名 14 个表
-- ============================================================

ALTER TABLE IF EXISTS rpt_category           RENAME TO erp_rpt_category;
ALTER TABLE IF EXISTS rpt_report             RENAME TO erp_rpt_report;
ALTER TABLE IF EXISTS rpt_field              RENAME TO erp_rpt_field;
ALTER TABLE IF EXISTS rpt_filter             RENAME TO erp_rpt_filter;
ALTER TABLE IF EXISTS rpt_sort               RENAME TO erp_rpt_sort;
ALTER TABLE IF EXISTS rpt_chart              RENAME TO erp_rpt_chart;
ALTER TABLE IF EXISTS rpt_permission         RENAME TO erp_rpt_permission;
ALTER TABLE IF EXISTS rpt_field_permission   RENAME TO erp_rpt_field_permission;
ALTER TABLE IF EXISTS rpt_bookmark           RENAME TO erp_rpt_bookmark;
ALTER TABLE IF EXISTS rpt_execution_log      RENAME TO erp_rpt_execution_log;
ALTER TABLE IF EXISTS rpt_datasource         RENAME TO erp_rpt_datasource;
ALTER TABLE IF EXISTS rpt_salesperson_map    RENAME TO erp_rpt_salesperson_map;
ALTER TABLE IF EXISTS rpt_pivot_view         RENAME TO erp_rpt_pivot_view;
ALTER TABLE IF EXISTS rpt_pivot_view_share   RENAME TO erp_rpt_pivot_view_share;

-- ============================================================
-- 步骤 4：重命名相关索引
-- ============================================================

-- rpt_category
ALTER INDEX IF EXISTS rpt_category_pkey               RENAME TO erp_rpt_category_pkey;
ALTER INDEX IF EXISTS idx_rpt_category_parent          RENAME TO idx_erp_rpt_category_parent;

-- rpt_report
ALTER INDEX IF EXISTS rpt_report_pkey                  RENAME TO erp_rpt_report_pkey;
ALTER INDEX IF EXISTS idx_rpt_report_name              RENAME TO idx_erp_rpt_report_name;
ALTER INDEX IF EXISTS idx_rpt_report_category          RENAME TO idx_erp_rpt_report_category;
ALTER INDEX IF EXISTS idx_rpt_report_status            RENAME TO idx_erp_rpt_report_status;

-- rpt_field
ALTER INDEX IF EXISTS rpt_field_pkey                   RENAME TO erp_rpt_field_pkey;
ALTER INDEX IF EXISTS idx_rpt_field_report             RENAME TO idx_erp_rpt_field_report;

-- rpt_filter
ALTER INDEX IF EXISTS rpt_filter_pkey                  RENAME TO erp_rpt_filter_pkey;
ALTER INDEX IF EXISTS idx_rpt_filter_report            RENAME TO idx_erp_rpt_filter_report;

-- rpt_sort
ALTER INDEX IF EXISTS rpt_sort_pkey                    RENAME TO erp_rpt_sort_pkey;
ALTER INDEX IF EXISTS idx_rpt_sort_report              RENAME TO idx_erp_rpt_sort_report;

-- rpt_chart
ALTER INDEX IF EXISTS rpt_chart_pkey                   RENAME TO erp_rpt_chart_pkey;
ALTER INDEX IF EXISTS idx_rpt_chart_report             RENAME TO idx_erp_rpt_chart_report;

-- rpt_permission
ALTER INDEX IF EXISTS rpt_permission_pkey              RENAME TO erp_rpt_permission_pkey;
ALTER INDEX IF EXISTS idx_rpt_permission_uniq          RENAME TO idx_erp_rpt_permission_uniq;

-- rpt_field_permission
ALTER INDEX IF EXISTS rpt_field_permission_pkey        RENAME TO erp_rpt_field_permission_pkey;
ALTER INDEX IF EXISTS idx_rpt_field_permission_uniq    RENAME TO idx_erp_rpt_field_permission_uniq;

-- rpt_bookmark
ALTER INDEX IF EXISTS rpt_bookmark_pkey                RENAME TO erp_rpt_bookmark_pkey;
ALTER INDEX IF EXISTS idx_rpt_bookmark_uniq            RENAME TO idx_erp_rpt_bookmark_uniq;
ALTER INDEX IF EXISTS idx_rpt_bookmark_user            RENAME TO idx_erp_rpt_bookmark_user;

-- rpt_execution_log
ALTER INDEX IF EXISTS rpt_execution_log_pkey           RENAME TO erp_rpt_execution_log_pkey;
ALTER INDEX IF EXISTS idx_rpt_execution_log_report     RENAME TO idx_erp_rpt_execution_log_report;
ALTER INDEX IF EXISTS idx_rpt_execution_log_user       RENAME TO idx_erp_rpt_execution_log_user;

-- rpt_datasource
ALTER INDEX IF EXISTS rpt_datasource_pkey              RENAME TO erp_rpt_datasource_pkey;
ALTER INDEX IF EXISTS idx_rpt_datasource_name          RENAME TO idx_erp_rpt_datasource_name;
ALTER INDEX IF EXISTS idx_rpt_datasource_enabled       RENAME TO idx_erp_rpt_datasource_enabled;

-- rpt_salesperson_map
ALTER INDEX IF EXISTS rpt_salesperson_map_pkey         RENAME TO erp_rpt_salesperson_map_pkey;
ALTER INDEX IF EXISTS idx_rpt_salesperson_map_code     RENAME TO idx_erp_rpt_salesperson_map_code;

-- rpt_pivot_view
ALTER INDEX IF EXISTS rpt_pivot_view_pkey              RENAME TO erp_rpt_pivot_view_pkey;

-- rpt_pivot_view_share
ALTER INDEX IF EXISTS rpt_pivot_view_share_pkey        RENAME TO erp_rpt_pivot_view_share_pkey;
ALTER INDEX IF EXISTS idx_rpt_pivot_view_share_target  RENAME TO idx_erp_rpt_pivot_view_share_target;

-- ============================================================
-- 步骤 5：更新表注释
-- ============================================================

COMMENT ON TABLE erp_rpt_category         IS '报表分类（由 Migration 034 重命名自 rpt_category）';
COMMENT ON TABLE erp_rpt_report           IS '报表定义（由 Migration 034 重命名自 rpt_report）';
COMMENT ON TABLE erp_rpt_field            IS '报表字段配置（由 Migration 034 重命名自 rpt_field）';
COMMENT ON TABLE erp_rpt_filter           IS '报表筛选条件（由 Migration 034 重命名自 rpt_filter）';
COMMENT ON TABLE erp_rpt_sort             IS '报表排序配置（由 Migration 034 重命名自 rpt_sort）';
COMMENT ON TABLE erp_rpt_chart            IS '报表图表配置（由 Migration 034 重命名自 rpt_chart）';
COMMENT ON TABLE erp_rpt_permission       IS '报表权限（由 Migration 034 重命名自 rpt_permission）';
COMMENT ON TABLE erp_rpt_field_permission IS '报表字段级权限（由 Migration 034 重命名自 rpt_field_permission）';
COMMENT ON TABLE erp_rpt_bookmark         IS '报表书签（由 Migration 034 重命名自 rpt_bookmark）';
COMMENT ON TABLE erp_rpt_execution_log    IS '报表执行日志（由 Migration 034 重命名自 rpt_execution_log）';
COMMENT ON TABLE erp_rpt_datasource       IS '报表数据源配置（由 Migration 034 重命名自 rpt_datasource）';
COMMENT ON TABLE erp_rpt_salesperson_map  IS '员工-业务员映射表（由 Migration 034 重命名自 rpt_salesperson_map）';
COMMENT ON TABLE erp_rpt_pivot_view       IS '透视表配置（由 Migration 034 重命名自 rpt_pivot_view）';
COMMENT ON TABLE erp_rpt_pivot_view_share IS '透视表共享目标（由 Migration 034 重命名自 rpt_pivot_view_share）';

-- ============================================================
-- 步骤 6：验证
-- ============================================================

DO $$
DECLARE
    tbl RECORD;
    old_name TEXT;
    new_name TEXT;
    old_exists BOOLEAN;
    new_exists BOOLEAN;
    row_count INT;
    total_ok INT := 0;
    total_fail INT := 0;
BEGIN
    FOR tbl IN
        SELECT unnest(ARRAY[
            'rpt_category', 'rpt_report', 'rpt_field', 'rpt_filter',
            'rpt_sort', 'rpt_chart', 'rpt_permission', 'rpt_field_permission',
            'rpt_bookmark', 'rpt_execution_log', 'rpt_datasource',
            'rpt_salesperson_map', 'rpt_pivot_view', 'rpt_pivot_view_share'
        ]) AS old_name
    LOOP
        new_name := 'erp_' || tbl.old_name;
        SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = tbl.old_name AND relkind = 'r') INTO old_exists;
        SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = new_name AND relkind = 'r') INTO new_exists;

        IF new_exists AND NOT old_exists THEN
            EXECUTE FORMAT('SELECT COUNT(*) FROM %I', new_name) INTO row_count;
            RAISE NOTICE '✅ % → %（% 条记录）', tbl.old_name, new_name, row_count;
            total_ok := total_ok + 1;
        ELSE
            RAISE WARNING '❌ % 重命名失败（old_exists=%, new_exists=%）', tbl.old_name, old_exists, new_exists;
            total_fail := total_fail + 1;
        END IF;
    END LOOP;

    RAISE NOTICE '──────────────────────────────────────────';
    RAISE NOTICE '完成：成功 % / 14，失败 % / 14', total_ok, total_fail;
END $$;