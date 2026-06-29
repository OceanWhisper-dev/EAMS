-- 013_ImportLegacyEventDeclareds.sql
-- 从原系统(EMData2 SQL Server)导入2026年考勤事件数据
-- 使用步骤：
--   1. 先在SQL Server中导出数据（如已导出可跳过）：
--      sqlcmd -S "DB" -d "EMData2" -U "sa" -P "dfl_DB" -Q "
--        SET NOCOUNT ON;
--        SELECT ed.EventId, ISNULL(ed.EventDescription,''), ISNULL(CAST(ed.Fee AS DECIMAL(10,2)),0.00),
--               ISNULL(ed.Memo,''), ISNULL(ed.checkMan,''), ISNULL(ed.Manager,''),
--               ed.recordID, CASE WHEN ed.isBeginTime=1 THEN 1 ELSE 0 END,
--               r.EmployeeId, CONVERT(varchar(10),r.SDate,23), r.PeriodNo
--        FROM Attendance2_EventDeclared ed
--        INNER JOIN Attendance2_Records r ON ed.RecordId = r.RecordId
--        WHERE YEAR(r.SDate)=2026
--        ORDER BY r.EmployeeId, r.SDate, ed.EventId
--      " -o "temp_old_events_2026.csv" -s"|" -W -w 8000 -h-1
--   2. 将CSV文件复制到PostgreSQL可访问的路径
--   3. 修改下面的 CSV_PATH 为实际路径
--   4. 执行本脚本

BEGIN;

-- ============================================================
-- 配置：CSV文件路径（根据实际情况修改）
-- ============================================================
-- \set CSV_PATH '''D:\\Codes\\Projects\\eams2026\\temp_old_events_2026.csv'''

-- ============================================================
-- 1. 创建临时暂存表
-- ============================================================
DROP TABLE IF EXISTS _tmp_old_events_2026;

CREATE TEMP TABLE _tmp_old_events_2026 (
    old_event_id      INTEGER NOT NULL,
    event_description VARCHAR(500),
    fee               DECIMAL(10,2),
    memo              VARCHAR(500),
    check_man         VARCHAR(128),
    manager           VARCHAR(128),
    old_record_id     INTEGER NOT NULL,
    is_begin_time     SMALLINT NOT NULL,
    old_employee_id   INTEGER NOT NULL,
    s_date            DATE NOT NULL,
    period_no         INTEGER NOT NULL
);

-- ============================================================
-- 2. 从CSV导入暂存表
-- ============================================================
COPY _tmp_old_events_2026 (old_event_id, event_description, fee, memo, check_man, manager, old_record_id, is_begin_time, old_employee_id, s_date, period_no)
FROM 'D:\Codes\Projects\eams2026\temp_old_events_2026_utf8.csv'
DELIMITER '|'
NULL ''
CSV;

-- ============================================================
-- 3. 验证CSV导入结果
-- ============================================================
SELECT 'CSV导入记录数' as step, COUNT(*) FROM _tmp_old_events_2026;

-- ============================================================
-- 4. 确认新旧系统的员工映射关系是否完整
-- ============================================================
SELECT '员工匹配情况' as step, 
       COUNT(*) AS 总事件数,
       COUNT(CASE WHEN ae.id IS NOT NULL THEN 1 END) AS 已匹配员工,
       COUNT(CASE WHEN ae.id IS NULL THEN 1 END) AS 未匹配员工
FROM _tmp_old_events_2026 tmp
LEFT JOIN attendance_employees ae ON ae.hwatt_employee_id = tmp.old_employee_id
    AND ae.is_deleted = FALSE;

-- 显示未匹配的员工ID（如有）
SELECT DISTINCT tmp.old_employee_id
FROM _tmp_old_events_2026 tmp
LEFT JOIN attendance_employees ae ON ae.hwatt_employee_id = tmp.old_employee_id
    AND ae.is_deleted = FALSE
WHERE ae.id IS NULL;

-- ============================================================
-- 5. 确认旧考勤记录在新系统中是否存在
-- ============================================================
SELECT '考勤记录匹配情况' as step,
       COUNT(*) AS 总事件数,
       COUNT(CASE WHEN r.id IS NOT NULL THEN 1 END) AS 已匹配记录,
       COUNT(CASE WHEN r.id IS NULL THEN 1 END) AS 未匹配记录
FROM _tmp_old_events_2026 tmp
LEFT JOIN attendance_employees ae ON ae.hwatt_employee_id = tmp.old_employee_id
    AND ae.is_deleted = FALSE
LEFT JOIN attendance_records r ON r.employee_id = ae.employee_id
    AND r.s_date = tmp.s_date
    AND r.is_deleted = FALSE;

-- ============================================================
-- 6. 执行迁移：插入考勤事件数据
-- ============================================================
WITH mapped AS (
    SELECT DISTINCT ON (tmp.old_event_id)
        tmp.old_event_id,
        tmp.event_description,
        tmp.fee,
        tmp.memo,
        tmp.check_man,
        tmp.manager,
        r.id AS new_record_id,
        tmp.is_begin_time::int::boolean,
        r.created_by
    FROM _tmp_old_events_2026 tmp
    INNER JOIN attendance_employees ae
        ON ae.hwatt_employee_id = tmp.old_employee_id
        AND ae.is_deleted = FALSE
    INNER JOIN attendance_records r
        ON r.employee_id = ae.employee_id
        AND r.s_date = tmp.s_date
        AND r.is_deleted = FALSE
)
INSERT INTO attendance_event_declareds
    (event_description, fee, memo, check_man, manager,
     record_id, is_begin_time, created_at, created_by,
     updated_at, updated_by, is_deleted)
SELECT
    m.event_description,
    CASE WHEN m.fee = 0 THEN NULL ELSE m.fee END,
    NULLIF(m.memo, ''),
    NULLIF(m.check_man, ''),
    NULLIF(m.manager, ''),
    m.new_record_id,
    m.is_begin_time,
    NOW(), COALESCE(m.created_by, 1),
    NOW(), COALESCE(m.created_by, 1),
    FALSE
FROM mapped m
-- 避免重复导入
WHERE NOT EXISTS (
    SELECT 1 FROM attendance_event_declareds ed
    WHERE ed.record_id = m.new_record_id
      AND ed.event_description IS NOT DISTINCT FROM m.event_description
      AND ed.fee IS NOT DISTINCT FROM CASE WHEN m.fee = 0 THEN NULL ELSE m.fee END
);

-- ============================================================
-- 7. 统计迁移结果
-- ============================================================
SELECT '迁移结果' as step,
       COUNT(*) AS 成功导入数
FROM attendance_event_declareds ed
WHERE ed.created_at >= NOW() - INTERVAL '1 minute'
  AND ed.is_deleted = FALSE;

-- ============================================================
-- 8. 清理临时表
-- ============================================================
DROP TABLE IF EXISTS _tmp_old_events_2026;

COMMIT;