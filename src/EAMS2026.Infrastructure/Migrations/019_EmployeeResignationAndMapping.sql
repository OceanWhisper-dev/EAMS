-- 019_EmployeeResignationAndMapping.sql
-- 员工管理增加离职日期 + 考勤员工关联系统员工

ALTER TABLE employees ADD COLUMN IF NOT EXISTS resignation_date DATE NULL;

COMMENT ON COLUMN employees.resignation_date IS '离职日期，NULL表示在职';

ALTER TABLE attendance_employees ADD COLUMN IF NOT EXISTS system_employee_id BIGINT NULL;

COMMENT ON COLUMN attendance_employees.system_employee_id IS '关联系统员工表(employees.id)，NULL表示未匹配';

CREATE INDEX IF NOT EXISTS idx_attendance_employees_system_employee_id ON attendance_employees(system_employee_id);