-- 增强仪表盘组件表结构
ALTER TABLE dashboard_widgets ADD COLUMN IF NOT EXISTS data_source_type VARCHAR(50) DEFAULT 'sql';
ALTER TABLE dashboard_widgets ADD COLUMN IF NOT EXISTS data_source_config JSONB;
ALTER TABLE dashboard_widgets ADD COLUMN IF NOT EXISTS layout_config JSONB DEFAULT '{"span": 24}';
ALTER TABLE dashboard_widgets ADD COLUMN IF NOT EXISTS refresh_interval INT DEFAULT 0;

-- 更新现有数据
UPDATE dashboard_widgets SET 
    data_source_type = 'builtin',
    data_source_config = (CASE widget_key
        WHEN 'stat_departments' THEN '{"type": "count", "table": "sys_departments", "condition": "is_deleted = FALSE"}'
        WHEN 'stat_employees' THEN '{"type": "count", "table": "sys_employees", "condition": "is_deleted = FALSE"}'
        WHEN 'stat_users' THEN '{"type": "count", "table": "sys_users", "condition": "is_deleted = FALSE"}'
        WHEN 'stat_roles' THEN '{"type": "count", "table": "sys_roles", "condition": "is_deleted = FALSE"}'
        WHEN 'stat_messages' THEN '{"type": "unread_messages"}'
        WHEN 'recent_logs' THEN '{"type": "recent_logs", "limit": 10}'
        ELSE '{}'
    END)::jsonb,
    layout_config = (CASE widget_key
        WHEN 'welcome_card' THEN '{"span": 24}'
        WHEN 'quick_actions' THEN '{"span": 24}'
        ELSE '{"span": 6}'
    END)::jsonb
WHERE data_source_config IS NULL;
