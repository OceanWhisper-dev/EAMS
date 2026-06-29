-- ============================================================
-- EAMS2026 018: attendance_records 添加 fee 列
-- 目标数据库: PostgreSQL
-- 说明: 存储考勤事件费用合计，避免每次查询子查询
-- ============================================================

ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS fee DECIMAL(10, 2);
COMMENT ON COLUMN attendance_records.fee IS '考勤事件费用合计';