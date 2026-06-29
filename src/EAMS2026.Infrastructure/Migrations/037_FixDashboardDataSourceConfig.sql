-- ============================================================
-- 037_FixDashboardDataSourceConfig.sql
-- 修复仪表盘组件 data_source_config 中的旧表名
-- 表名重命名后，data_source_config 中引用的表名需要更新
-- ============================================================

-- 更新 stat_departments 的 data_source_config
UPDATE sys_dashboard_widgets 
SET data_source_config = '{"type": "count", "table": "sys_departments", "condition": "is_deleted = FALSE"}'
WHERE widget_key = 'stat_departments' 
  AND (data_source_config::jsonb) ->> 'table' = 'departments';

-- 更新 stat_employees 的 data_source_config
UPDATE sys_dashboard_widgets 
SET data_source_config = '{"type": "count", "table": "sys_employees", "condition": "is_deleted = FALSE"}'
WHERE widget_key = 'stat_employees' 
  AND (data_source_config::jsonb) ->> 'table' = 'employees';

-- 更新 stat_users 的 data_source_config
UPDATE sys_dashboard_widgets 
SET data_source_config = '{"type": "count", "table": "sys_users", "condition": "is_deleted = FALSE"}'
WHERE widget_key = 'stat_users' 
  AND (data_source_config::jsonb) ->> 'table' = 'users';

-- 更新 stat_roles 的 data_source_config
UPDATE sys_dashboard_widgets 
SET data_source_config = '{"type": "count", "table": "sys_roles", "condition": "is_deleted = FALSE"}'
WHERE widget_key = 'stat_roles' 
  AND (data_source_config::jsonb) ->> 'table' = 'roles';

-- 验证结果
SELECT widget_key, widget_name, data_source_config 
FROM sys_dashboard_widgets 
WHERE widget_key IN ('stat_departments', 'stat_employees', 'stat_users', 'stat_roles');