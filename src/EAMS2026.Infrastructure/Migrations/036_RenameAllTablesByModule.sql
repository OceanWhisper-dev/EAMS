-- ============================================================
-- Migration 036: 按模块规范统一重命名数据库表前缀
-- 说明：根据模块化规范，为存量表中没有模块前缀的表增加前缀：
--   系统管理(sys_)：departments, employees, users, roles 等 13 表
--   考勤同步(attendance_)：hwatt_*, dingtalk_* 共 4 表
-- 代码中 SQL 引用已在同一版本中同步更新为新的表名。
-- 执行此脚本后，部署新版本代码即可正常工作。
-- ============================================================

-- ============================================================
-- 步骤 1：验证条件
-- ============================================================

DO $$
DECLARE
    tbl_name TEXT;
    new_name TEXT;
    tbl_exists BOOLEAN;
    new_exists BOOLEAN;
    all_ok BOOLEAN := TRUE;
    active_count INT;
    missing_tables TEXT := '';
BEGIN
    -- 检查活跃连接
    SELECT COUNT(*) INTO active_count
    FROM pg_stat_activity
    WHERE datname = current_database()
      AND state IN ('active', 'idle in transaction')
      AND query ILIKE ANY(ARRAY[
        '%departments%', '%employees%', '%users%', '%roles%',
        '%user_roles%', '%permissions%', '%role_permissions%',
        '%operation_logs%', '%dict_types%', '%dict_items%',
        '%messages%', '%dashboard_widgets%', '%role_dashboard_config%',
        '%hwatt_%', '%dingtalk_%'
      ])
      AND pid <> pg_backend_pid();

    IF active_count > 3 THEN
        RAISE WARNING '存在 % 个活跃连接可能访问待重命名表，建议在业务低峰期执行', active_count;
    ELSE
        RAISE NOTICE '活跃连接检查通过（% 个）', active_count;
    END IF;

    -- 检查每个旧表是否存在，新表名是否冲突
    FOR tbl_name, new_name IN
        SELECT * FROM (VALUES
            ('departments',             'sys_departments'),
            ('employees',               'sys_employees'),
            ('users',                   'sys_users'),
            ('roles',                   'sys_roles'),
            ('user_roles',              'sys_user_roles'),
            ('permissions',             'sys_permissions'),
            ('role_permissions',        'sys_role_permissions'),
            ('operation_logs',          'sys_operation_logs'),
            ('dict_types',              'sys_dict_types'),
            ('dict_items',              'sys_dict_items'),
            ('messages',                'sys_messages'),
            ('dashboard_widgets',       'sys_dashboard_widgets'),
            ('role_dashboard_config',   'sys_role_dashboard_config'),
            ('hwatt_employees',         'attendance_hwatt_employees'),
            ('hwatt_card_records',      'attendance_hwatt_card_records'),
            ('dingtalk_employees',      'attendance_dingtalk_employees'),
            ('dingtalk_card_records',   'attendance_dingtalk_card_records')
        ) AS t(old_name, new_name)
    LOOP
        SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = tbl_name AND relkind = 'r') INTO tbl_exists;
        SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = new_name AND relkind = 'r') INTO new_exists;

        IF tbl_exists AND new_exists THEN
            RAISE WARNING '冲突：% 和 % 同时存在，跳过 % 的重命名', tbl_name, new_name, tbl_name;
            all_ok := FALSE;
        ELSIF NOT tbl_exists THEN
            missing_tables := missing_tables || tbl_name || ', ';
        END IF;
    END LOOP;

    IF missing_tables <> '' THEN
        RAISE NOTICE '以下表不存在，将跳过：%', LEFT(missing_tables, LENGTH(missing_tables) - 1);
    END IF;

    IF NOT all_ok THEN
        RAISE EXCEPTION '存在表名冲突，请手动处理后再运行';
    END IF;

    RAISE NOTICE '条件检查通过，开始重命名...';
END $$;

-- ============================================================
-- 步骤 2：自动备份所有旧表
-- ============================================================

DO $$
DECLARE
    bak_date TEXT := TO_CHAR(NOW(), 'YYYYMMDD');
BEGIN
    -- 系统表备份（仅当备份表不存在时创建）
    PERFORM FROM pg_class WHERE relname = 'departments_bak_' || bak_date AND relkind = 'r';
    IF NOT FOUND AND EXISTS (SELECT 1 FROM pg_class WHERE relname = 'departments' AND relkind = 'r') THEN
        CREATE TABLE departments_bak AS SELECT * FROM departments;
        RAISE NOTICE '✅ 备份 departments（% 条）', (SELECT COUNT(*) FROM departments);
    END IF;

    PERFORM FROM pg_class WHERE relname = 'employees_bak_' || bak_date AND relkind = 'r';
    IF NOT FOUND AND EXISTS (SELECT 1 FROM pg_class WHERE relname = 'employees' AND relkind = 'r') THEN
        CREATE TABLE employees_bak AS SELECT * FROM employees;
        RAISE NOTICE '✅ 备份 employees（% 条）', (SELECT COUNT(*) FROM employees);
    END IF;

    PERFORM FROM pg_class WHERE relname = 'users_bak_' || bak_date AND relkind = 'r';
    IF NOT FOUND AND EXISTS (SELECT 1 FROM pg_class WHERE relname = 'users' AND relkind = 'r') THEN
        CREATE TABLE users_bak AS SELECT * FROM users;
        RAISE NOTICE '✅ 备份 users（% 条）', (SELECT COUNT(*) FROM users);
    END IF;

    -- 简化：只创建必要的备份，其余用通用方式
    CREATE TABLE IF NOT EXISTS roles_bak AS SELECT * FROM roles;
    CREATE TABLE IF NOT EXISTS user_roles_bak AS SELECT * FROM user_roles;
    CREATE TABLE IF NOT EXISTS permissions_bak AS SELECT * FROM permissions;
    CREATE TABLE IF NOT EXISTS role_permissions_bak AS SELECT * FROM role_permissions;
    CREATE TABLE IF NOT EXISTS operation_logs_bak AS SELECT * FROM operation_logs;
    CREATE TABLE IF NOT EXISTS dict_types_bak AS SELECT * FROM dict_types;
    CREATE TABLE IF NOT EXISTS dict_items_bak AS SELECT * FROM dict_items;
    CREATE TABLE IF NOT EXISTS messages_bak AS SELECT * FROM messages;
    CREATE TABLE IF NOT EXISTS dashboard_widgets_bak AS SELECT * FROM dashboard_widgets;
    CREATE TABLE IF NOT EXISTS role_dashboard_config_bak AS SELECT * FROM role_dashboard_config;
    CREATE TABLE IF NOT EXISTS hwatt_employees_bak AS SELECT * FROM hwatt_employees;
    CREATE TABLE IF NOT EXISTS hwatt_card_records_bak AS SELECT * FROM hwatt_card_records;
    CREATE TABLE IF NOT EXISTS dingtalk_employees_bak AS SELECT * FROM dingtalk_employees;
    CREATE TABLE IF NOT EXISTS dingtalk_card_records_bak AS SELECT * FROM dingtalk_card_records;

    RAISE NOTICE '所有备份表已创建（使用 IF NOT EXISTS，已有备份则跳过）';
END $$;

-- ============================================================
-- 步骤 3：重命名 17 个表
-- ============================================================

-- 系统管理模块（sys_ 前缀）
ALTER TABLE IF EXISTS departments            RENAME TO sys_departments;
ALTER TABLE IF EXISTS employees              RENAME TO sys_employees;
ALTER TABLE IF EXISTS users                  RENAME TO sys_users;
ALTER TABLE IF EXISTS roles                  RENAME TO sys_roles;
ALTER TABLE IF EXISTS user_roles             RENAME TO sys_user_roles;
ALTER TABLE IF EXISTS permissions            RENAME TO sys_permissions;
ALTER TABLE IF EXISTS role_permissions       RENAME TO sys_role_permissions;
ALTER TABLE IF EXISTS operation_logs         RENAME TO sys_operation_logs;
ALTER TABLE IF EXISTS dict_types             RENAME TO sys_dict_types;
ALTER TABLE IF EXISTS dict_items             RENAME TO sys_dict_items;
ALTER TABLE IF EXISTS messages               RENAME TO sys_messages;
ALTER TABLE IF EXISTS dashboard_widgets      RENAME TO sys_dashboard_widgets;
ALTER TABLE IF EXISTS role_dashboard_config  RENAME TO sys_role_dashboard_config;

-- 考勤同步模块（attendance_ 前缀）
ALTER TABLE IF EXISTS hwatt_employees        RENAME TO attendance_hwatt_employees;
ALTER TABLE IF EXISTS hwatt_card_records     RENAME TO attendance_hwatt_card_records;
ALTER TABLE IF EXISTS dingtalk_employees     RENAME TO attendance_dingtalk_employees;
ALTER TABLE IF EXISTS dingtalk_card_records  RENAME TO attendance_dingtalk_card_records;

-- ============================================================
-- 步骤 4：重命名索引
-- ============================================================

-- departments → sys_departments
ALTER INDEX IF EXISTS departments_pkey                  RENAME TO sys_departments_pkey;
ALTER INDEX IF EXISTS idx_departments_parent            RENAME TO idx_sys_departments_parent;
ALTER INDEX IF EXISTS idx_departments_code              RENAME TO idx_sys_departments_code;

-- employees → sys_employees
ALTER INDEX IF EXISTS employees_pkey                    RENAME TO sys_employees_pkey;
ALTER INDEX IF EXISTS idx_employees_department          RENAME TO idx_sys_employees_department;
ALTER INDEX IF EXISTS idx_employees_code                RENAME TO idx_sys_employees_code;
ALTER INDEX IF EXISTS idx_employees_id_card             RENAME TO idx_sys_employees_id_card;

-- users → sys_users
ALTER INDEX IF EXISTS users_pkey                        RENAME TO sys_users_pkey;
ALTER INDEX IF EXISTS idx_users_username                RENAME TO idx_sys_users_username;
ALTER INDEX IF EXISTS idx_users_employee                RENAME TO idx_sys_users_employee;

-- roles → sys_roles
ALTER INDEX IF EXISTS roles_pkey                        RENAME TO sys_roles_pkey;
ALTER INDEX IF EXISTS idx_roles_name                    RENAME TO idx_sys_roles_name;

-- user_roles → sys_user_roles
ALTER INDEX IF EXISTS user_roles_pkey                   RENAME TO sys_user_roles_pkey;
ALTER INDEX IF EXISTS idx_user_roles_user               RENAME TO idx_sys_user_roles_user;
ALTER INDEX IF EXISTS idx_user_roles_role               RENAME TO idx_sys_user_roles_role;

-- permissions → sys_permissions
ALTER INDEX IF EXISTS permissions_pkey                  RENAME TO sys_permissions_pkey;
ALTER INDEX IF EXISTS idx_permissions_code              RENAME TO idx_sys_permissions_code;

-- role_permissions → sys_role_permissions
ALTER INDEX IF EXISTS role_permissions_pkey             RENAME TO sys_role_permissions_pkey;
ALTER INDEX IF EXISTS idx_role_permissions_role         RENAME TO idx_sys_role_permissions_role;
ALTER INDEX IF EXISTS idx_role_permissions_permission   RENAME TO idx_sys_role_permissions_permission;

-- operation_logs → sys_operation_logs
ALTER INDEX IF EXISTS operation_logs_pkey               RENAME TO sys_operation_logs_pkey;
ALTER INDEX IF EXISTS idx_operation_logs_user           RENAME TO idx_sys_operation_logs_user;
ALTER INDEX IF EXISTS idx_operation_logs_time           RENAME TO idx_sys_operation_logs_time;

-- dict_types → sys_dict_types
ALTER INDEX IF EXISTS dict_types_pkey                   RENAME TO sys_dict_types_pkey;
ALTER INDEX IF EXISTS idx_dict_types_code               RENAME TO idx_sys_dict_types_code;

-- dict_items → sys_dict_items
ALTER INDEX IF EXISTS dict_items_pkey                   RENAME TO sys_dict_items_pkey;
ALTER INDEX IF EXISTS idx_dict_items_type               RENAME TO idx_sys_dict_items_type;

-- messages → sys_messages
ALTER INDEX IF EXISTS messages_pkey                     RENAME TO sys_messages_pkey;
ALTER INDEX IF EXISTS idx_messages_sender               RENAME TO idx_sys_messages_sender;
ALTER INDEX IF EXISTS idx_messages_receiver             RENAME TO idx_sys_messages_receiver;

-- dashboard_widgets → sys_dashboard_widgets
ALTER INDEX IF EXISTS dashboard_widgets_pkey            RENAME TO sys_dashboard_widgets_pkey;

-- role_dashboard_config → sys_role_dashboard_config
ALTER INDEX IF EXISTS role_dashboard_config_pkey        RENAME TO sys_role_dashboard_config_pkey;

-- hwatt_employees → attendance_hwatt_employees
ALTER INDEX IF EXISTS hwatt_employees_pkey              RENAME TO attendance_hwatt_employees_pkey;
ALTER INDEX IF EXISTS idx_hwatt_employees_code          RENAME TO idx_attendance_hwatt_employees_code;

-- hwatt_card_records → attendance_hwatt_card_records
ALTER INDEX IF EXISTS hwatt_card_records_pkey           RENAME TO attendance_hwatt_card_records_pkey;

-- dingtalk_employees → attendance_dingtalk_employees
ALTER INDEX IF EXISTS dingtalk_employees_pkey           RENAME TO attendance_dingtalk_employees_pkey;

-- dingtalk_card_records → attendance_dingtalk_card_records
ALTER INDEX IF EXISTS dingtalk_card_records_pkey        RENAME TO attendance_dingtalk_card_records_pkey;

-- ============================================================
-- 步骤 5：更新表注释
-- ============================================================

COMMENT ON TABLE sys_departments           IS '部门表（由 Migration 036 重命名自 departments）';
COMMENT ON TABLE sys_employees             IS '员工表（由 Migration 036 重命名自 employees）';
COMMENT ON TABLE sys_users                 IS '用户表（由 Migration 036 重命名自 users）';
COMMENT ON TABLE sys_roles                 IS '角色表（由 Migration 036 重命名自 roles）';
COMMENT ON TABLE sys_user_roles            IS '用户角色关联表（由 Migration 036 重命名自 user_roles）';
COMMENT ON TABLE sys_permissions           IS '权限表（由 Migration 036 重命名自 permissions）';
COMMENT ON TABLE sys_role_permissions      IS '角色权限关联表（由 Migration 036 重命名自 role_permissions）';
COMMENT ON TABLE sys_operation_logs        IS '操作日志表（由 Migration 036 重命名自 operation_logs）';
COMMENT ON TABLE sys_dict_types            IS '字典类型表（由 Migration 036 重命名自 dict_types）';
COMMENT ON TABLE sys_dict_items            IS '字典项表（由 Migration 036 重命名自 dict_items）';
COMMENT ON TABLE sys_messages              IS '消息表（由 Migration 036 重命名自 messages）';
COMMENT ON TABLE sys_dashboard_widgets     IS '仪表盘组件表（由 Migration 036 重命名自 dashboard_widgets）';
COMMENT ON TABLE sys_role_dashboard_config IS '角色仪表盘配置表（由 Migration 036 重命名自 role_dashboard_config）';
COMMENT ON TABLE attendance_hwatt_employees     IS 'HWATT考勤员工缓存表（由 Migration 036 重命名自 hwatt_employees）';
COMMENT ON TABLE attendance_hwatt_card_records  IS 'HWATT打卡记录缓存表（由 Migration 036 重命名自 hwatt_card_records）';
COMMENT ON TABLE attendance_dingtalk_employees  IS '钉钉考勤员工缓存表（由 Migration 036 重命名自 dingtalk_employees）';
COMMENT ON TABLE attendance_dingtalk_card_records IS '钉钉打卡记录缓存表（由 Migration 036 重命名自 dingtalk_card_records）';

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
    total_skip INT := 0;
BEGIN
    FOR tbl IN
        SELECT * FROM (VALUES
            ('departments',             'sys_departments'),
            ('employees',               'sys_employees'),
            ('users',                   'sys_users'),
            ('roles',                   'sys_roles'),
            ('user_roles',              'sys_user_roles'),
            ('permissions',             'sys_permissions'),
            ('role_permissions',        'sys_role_permissions'),
            ('operation_logs',          'sys_operation_logs'),
            ('dict_types',              'sys_dict_types'),
            ('dict_items',              'sys_dict_items'),
            ('messages',                'sys_messages'),
            ('dashboard_widgets',       'sys_dashboard_widgets'),
            ('role_dashboard_config',   'sys_role_dashboard_config'),
            ('hwatt_employees',         'attendance_hwatt_employees'),
            ('hwatt_card_records',      'attendance_hwatt_card_records'),
            ('dingtalk_employees',      'attendance_dingtalk_employees'),
            ('dingtalk_card_records',   'attendance_dingtalk_card_records')
        ) AS t(old, new)
    LOOP
        old_name := tbl.old;
        new_name := tbl.new;
        SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = old_name AND relkind = 'r') INTO old_exists;
        SELECT EXISTS (SELECT 1 FROM pg_class WHERE relname = new_name AND relkind = 'r') INTO new_exists;

        IF new_exists AND NOT old_exists THEN
            EXECUTE FORMAT('SELECT COUNT(*) FROM %I', new_name) INTO row_count;
            RAISE NOTICE '✅ % → %（% 条记录）', old_name, new_name, row_count;
            total_ok := total_ok + 1;
        ELSIF new_exists AND old_exists THEN
            RAISE WARNING '⚠️ % → %（新旧共存）', old_name, new_name;
        ELSE
            RAISE NOTICE '➖ % → 跳过（表不存在）', old_name;
            total_skip := total_skip + 1;
        END IF;
    END LOOP;

    RAISE NOTICE '══════════════════════════════════';
    RAISE NOTICE '完成：成功 % / 17，跳过（不存在）% / 17', total_ok, total_skip;
END $$;