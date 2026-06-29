WITH attendance_module AS (SELECT id FROM permissions WHERE code = 'attendance' LIMIT 1),
new_perms AS (
    INSERT INTO permissions (name, code, type, parent_id, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
    VALUES 
        ('Import All Attendance', 'attendance:import-all', 'button', (SELECT id FROM attendance_module), 1, FALSE, NOW(), 1, NOW(), 1),
        ('Import Attendance', 'attendance:import', 'button', (SELECT id FROM attendance_module), 2, FALSE, NOW(), 1, NOW(), 1),
        ('Print Report', 'attendance:print', 'button', (SELECT id FROM attendance_module), 3, FALSE, NOW(), 1, NOW(), 1),
        ('Export Report', 'attendance:export', 'button', (SELECT id FROM attendance_module), 4, FALSE, NOW(), 1, NOW(), 1),
        ('Sync HWATT Employees', 'attendance:sync-employees', 'button', (SELECT id FROM attendance_module), 5, FALSE, NOW(), 1, NOW(), 1)
    RETURNING id
)
INSERT INTO role_permissions (role_id, permission_id)
SELECT 1, id FROM new_perms
ON CONFLICT DO NOTHING;