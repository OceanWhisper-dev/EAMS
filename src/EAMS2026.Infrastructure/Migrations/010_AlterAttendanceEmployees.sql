-- 为 attendance_employees 表添加双数据源支持字段
-- 用于区分员工数据来源（HWATT考勤机 / 钉钉API）

ALTER TABLE attendance_employees ADD COLUMN IF NOT EXISTS source VARCHAR(20) DEFAULT 'hwatt';
ALTER TABLE attendance_employees ADD COLUMN IF NOT EXISTS hwatt_employee_id BIGINT;

COMMENT ON COLUMN attendance_employees.source IS '数据来源: hwatt=考勤机, dingtalk=钉钉';
COMMENT ON COLUMN attendance_employees.hwatt_employee_id IS 'HWATT系统员工ID(仅source=hwatt时有值)';

CREATE INDEX IF NOT EXISTS idx_attendance_employees_source ON attendance_employees(source);