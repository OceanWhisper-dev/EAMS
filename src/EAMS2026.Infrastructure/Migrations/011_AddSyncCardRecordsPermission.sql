-- 添加同步HWATT打卡记录权限
WITH attendance_module AS (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1),
new_perms AS (
    INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
    VALUES 
        ('Sync HWATT Card Records', 'attendance:sync-card-records', 'button', (SELECT id FROM attendance_module), 6, FALSE, NOW(), 1, NOW(), 1)
    RETURNING id
)
INSERT INTO role_permissions (role_id, permission_id)
SELECT 1, id FROM new_perms
ON CONFLICT DO NOTHING;