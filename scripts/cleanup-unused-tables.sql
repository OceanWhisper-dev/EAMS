-- ============================================================
-- 清理不再使用的数据库表
-- 目标数据库: PostgreSQL (eams2026)
-- 生成日期: 2026-06-30
--
-- 验证方式：
--   已通过 psql 连接实际数据库确认以下表均存在且无代码引用：
--   psql -h localhost -U postgres -d eams2026 -c "SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename;"
--   代码引用搜索：grep -r "FROM\s\+table_name" src/ 遍历所有 Repository 层 SQL
--
-- 使用方式：
--   逐段取消注释执行，或一次全部执行
-- ============================================================

-- ============================================================
-- 第一部分：废弃表（已由 Migration 035 标记 obsolete_ 前缀）
-- 由 009/014 迁移创建，代码中完全无引用
-- ============================================================

-- 1. obsolete_hwatt_sync_logs — HWATT 同步日志，0 代码引用
DROP TABLE IF EXISTS obsolete_hwatt_sync_logs CASCADE;

-- 2. obsolete_dingtalk_sync_logs — 钉钉同步日志，0 代码引用
DROP TABLE IF EXISTS obsolete_dingtalk_sync_logs CASCADE;

-- ============================================================
-- 第二部分：Migration 035 数据备份表
-- （先备份再重命名，数据已保留在备份表中）
-- ============================================================

DROP TABLE IF EXISTS hwatt_sync_logs_bak CASCADE;
DROP TABLE IF EXISTS dingtalk_sync_logs_bak CASCADE;

-- ============================================================
-- 第三部分：Migration 036 备份表（17 张）
-- 重命名 sys_*/attendance_* 前缀前创建的备份，数据已迁移
-- ============================================================

DROP TABLE IF EXISTS departments_bak CASCADE;
DROP TABLE IF EXISTS employees_bak CASCADE;
DROP TABLE IF EXISTS users_bak CASCADE;
DROP TABLE IF EXISTS roles_bak CASCADE;
DROP TABLE IF EXISTS user_roles_bak CASCADE;
DROP TABLE IF EXISTS permissions_bak CASCADE;
DROP TABLE IF EXISTS role_permissions_bak CASCADE;
DROP TABLE IF EXISTS operation_logs_bak CASCADE;
DROP TABLE IF EXISTS dict_types_bak CASCADE;
DROP TABLE IF EXISTS dict_items_bak CASCADE;
DROP TABLE IF EXISTS messages_bak CASCADE;
DROP TABLE IF EXISTS dashboard_widgets_bak CASCADE;
DROP TABLE IF EXISTS role_dashboard_config_bak CASCADE;
DROP TABLE IF EXISTS hwatt_employees_bak CASCADE;
DROP TABLE IF EXISTS hwatt_card_records_bak CASCADE;
DROP TABLE IF EXISTS dingtalk_employees_bak CASCADE;
DROP TABLE IF EXISTS dingtalk_card_records_bak CASCADE;

-- ============================================================
-- 第四部分：Migration 034 备份表（rpt_ 旧前缀 + 日期后缀，14 张）
-- 正则匹配所有 rpt_xxx_bak_YYYYMMDD 格式的表
-- 当前实际存在后缀: _bak_20260617
-- ============================================================

DO $$
DECLARE
    tbl RECORD;
BEGIN
    FOR tbl IN
        SELECT relname
        FROM pg_class
        WHERE relkind = 'r'
          AND relname ~ '^rpt_.*_bak_\d{8}$'
          AND relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'public')
    LOOP
        EXECUTE FORMAT('DROP TABLE IF EXISTS %I CASCADE', tbl.relname);
        RAISE NOTICE '已删除备份表: %', tbl.relname;
    END LOOP;
END $$;

-- ============================================================
-- 第五部分：迁移遗留表（旧表名未清理干净）
-- ============================================================

-- 5.1 vouch_modify_logs（旧名）
--     Migration 030 创建时名为 erp_vouch_modify_logs，
--     但 an 未知来源又多出一张同结构旧名表（无 erp_ 前缀）。
--     代码只引用 erp_vouch_modify_logs，此旧名表无任何操作。
--     数据量: 80 kB，如无重要数据可安全删除。
DROP TABLE IF EXISTS vouch_modify_logs CASCADE;

-- 5.2 vouch_modify_templates
--     数据库中存在但未在任何迁移 SQL 或 C# 代码中找到创建语句和引用。
--     数据量: 56 kB，经用户确认无用，可安全删除。
DROP TABLE IF EXISTS vouch_modify_templates CASCADE;

-- ============================================================
-- 验证：确认上述指定表已全部删除
-- ============================================================

DO $$
DECLARE
    remaining TEXT;
BEGIN
    SELECT string_agg(relname, ', ')
    INTO remaining
    FROM pg_class
    WHERE relkind = 'r'
      AND relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'public')
      AND relname IN (
        'obsolete_hwatt_sync_logs',
        'obsolete_dingtalk_sync_logs',
        'hwatt_sync_logs_bak',
        'dingtalk_sync_logs_bak',
        'departments_bak',
        'employees_bak',
        'users_bak',
        'roles_bak',
        'user_roles_bak',
        'permissions_bak',
        'role_permissions_bak',
        'operation_logs_bak',
        'dict_types_bak',
        'dict_items_bak',
        'messages_bak',
        'dashboard_widgets_bak',
        'role_dashboard_config_bak',
        'hwatt_employees_bak',
        'hwatt_card_records_bak',
        'dingtalk_employees_bak',
        'dingtalk_card_records_bak',
        'vouch_modify_logs',
        'vouch_modify_templates'
      );

    IF remaining IS NULL THEN
        RAISE NOTICE '✅ 所有指定表已删除';
    ELSE
        RAISE WARNING '⚠️ 以下表仍存在: %', remaining;
    END IF;

    -- 额外检查 rpt_ 备份表是否还有残留
    IF EXISTS (SELECT 1 FROM pg_class WHERE relkind = 'r' AND relname ~ '^rpt_.*_bak_\d{8}$') THEN
        RAISE WARNING '⚠️ 仍有 rpt_ 备份表残留，请检查';
    ELSE
        RAISE NOTICE '✅ rpt_ 备份表也已全部删除';
    END IF;
END $$;

-- ============================================================
-- 附录：数据库全表清单（执行前请确认）
-- 可通过以下命令查看当前所有表：
--   psql -h localhost -U postgres -d eams2026 -c "SELECT tablename, pg_size_pretty(pg_total_relation_size('public.'||tablename)) AS size FROM pg_tables WHERE schemaname='public' ORDER BY tablename;"
-- ============================================================
