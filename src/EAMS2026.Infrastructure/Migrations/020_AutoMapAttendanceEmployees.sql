-- 020_AutoMapAttendanceEmployees.sql
-- 自动映射考勤员工到系统员工
-- 规则：
--   HWATT 来源：employee_name → employees.name
--   钉钉 来源：employee_name → employees.name
-- 仅对 system_employee_id IS NULL 的考勤员工执行，不覆盖已有手工映射

-- ============================================================
-- 1. 映射 HWATT 考勤员工（按姓名匹配）
-- ============================================================
UPDATE attendance_employees ae
SET system_employee_id = e.id,
    updated_at = NOW(),
    updated_by = 1
FROM employees e
WHERE ae.source = 'hwatt'
  AND ae.system_employee_id IS NULL
  AND e.is_deleted = FALSE
  AND e.status = TRUE
  AND (e.resignation_date IS NULL OR e.resignation_date > CURRENT_DATE)
  AND ae.employee_name::varchar = e.name;

-- ============================================================
-- 2. 映射钉钉考勤员工（按姓名匹配）
-- ============================================================
UPDATE attendance_employees ae
SET system_employee_id = e.id,
    updated_at = NOW(),
    updated_by = 1
FROM employees e
WHERE ae.source = 'dingtalk'
  AND ae.system_employee_id IS NULL
  AND e.is_deleted = FALSE
  AND e.status = TRUE
  AND (e.resignation_date IS NULL OR e.resignation_date > CURRENT_DATE)
  AND ae.employee_name = e.name;

-- ============================================================
-- 3. 输出映射统计
-- ============================================================
DO $$
DECLARE
    hwatt_count INT;
    dingtalk_count INT;
BEGIN
    SELECT COUNT(*) INTO hwatt_count FROM attendance_employees
    WHERE source = 'hwatt' AND system_employee_id IS NOT NULL;

    SELECT COUNT(*) INTO dingtalk_count FROM attendance_employees
    WHERE source = 'dingtalk' AND system_employee_id IS NOT NULL;

    RAISE NOTICE 'HWATT 已映射: % 人', hwatt_count;
    RAISE NOTICE '钉钉 已映射: % 人', dingtalk_count;
    RAISE NOTICE '未映射 HWATT: % 人', (SELECT COUNT(*) FROM attendance_employees WHERE source = 'hwatt' AND system_employee_id IS NULL);
    RAISE NOTICE '未映射 钉钉: % 人', (SELECT COUNT(*) FROM attendance_employees WHERE source = 'dingtalk' AND system_employee_id IS NULL);
END $$;