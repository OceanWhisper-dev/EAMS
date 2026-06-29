CREATE TABLE IF NOT EXISTS sys_dashboard_widgets (
    id BIGSERIAL PRIMARY KEY,
    widget_key VARCHAR(100) NOT NULL UNIQUE,
    widget_name VARCHAR(100) NOT NULL,
    widget_type VARCHAR(50) NOT NULL,
    description TEXT,
    icon VARCHAR(50),
    default_config JSONB,
    sort_order INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS sys_role_dashboard_config (
    id BIGSERIAL PRIMARY KEY,
    role_id BIGINT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    widget_key VARCHAR(100) NOT NULL,
    is_enabled BOOLEAN DEFAULT TRUE,
    config JSONB,
    sort_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(role_id, widget_key)
);

INSERT INTO sys_dashboard_widgets (widget_key, widget_name, widget_type, description, icon, default_config, sort_order) VALUES
('stat_departments', '部门统计', 'stat_card', '显示部门总数', 'OfficeBuilding', '{"color": "#409eff", "api": "/dashboard/stats/departments"}', 1),
('stat_employees', '员工统计', 'stat_card', '显示员工总数', 'User', '{"color": "#67c23a", "api": "/dashboard/stats/employees"}', 2),
('stat_users', '用户统计', 'stat_card', '显示用户总数', 'Avatar', '{"color": "#e6a23c", "api": "/dashboard/stats/users"}', 3),
('stat_roles', '角色统计', 'stat_card', '显示角色总数', 'Key', '{"color": "#f56c6c", "api": "/dashboard/stats/roles"}', 4),
('stat_messages', '消息统计', 'stat_card', '显示未读消息数', 'Message', '{"color": "#909399", "api": "/dashboard/stats/unread_messages"}', 5),
('recent_logs', '最近操作日志', 'list', '显示最近10条操作日志', 'Document', '{"limit": 10, "api": "/operation-log?pageSize=10"}', 10),
('quick_actions', '快捷操作', 'quick_links', '常用功能快捷入口', 'Setting', '{"links": [{"label": "用户管理", "path": "/system/user"}, {"label": "员工管理", "path": "/system/employee"}, {"label": "部门管理", "path": "/system/department"}]}', 20),
('welcome_card', '欢迎卡片', 'info_card', '显示欢迎信息和用户信息', 'Sunrise', '{}', 0);
