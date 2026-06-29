-- 017_FixAttendanceRecordsDuplicate.sql
-- 清理 attendance_records 中的重复记录并添加唯一约束

-- 第1步：删除重复记录，每个员工每天只保留最新的那条
DELETE FROM attendance_records
WHERE id NOT IN (
    SELECT MAX(id)
    FROM attendance_records
    WHERE is_deleted = FALSE
    GROUP BY employee_id, s_date
);

-- 第2步：添加唯一约束，防止后续重复
ALTER TABLE attendance_records
ADD CONSTRAINT uq_attendance_records_employee_date
UNIQUE (employee_id, s_date);