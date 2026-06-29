-- 008_AttendanceTables.sql
-- 考勤管理模块数据库表结构

CREATE TABLE IF NOT EXISTS attendance_employees (
    id BIGSERIAL PRIMARY KEY,
    employee_id BIGINT NOT NULL,
    employee_name VARCHAR(100) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

CREATE TABLE IF NOT EXISTS attendance_day_types (
    id BIGSERIAL PRIMARY KEY,
    day_type_name VARCHAR(50) NOT NULL,
    day_type_caption VARCHAR(50),
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

CREATE TABLE IF NOT EXISTS attendance_scheme_classes (
    id BIGSERIAL PRIMARY KEY,
    class_name VARCHAR(100) NOT NULL,
    periods INTEGER,
    class_description VARCHAR(500),
    class_periods INTEGER,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

CREATE TABLE IF NOT EXISTS attendance_plan_times (
    id BIGSERIAL PRIMARY KEY,
    plan_name VARCHAR(100) NOT NULL,
    day_type_id BIGINT NOT NULL,
    description VARCHAR(500),
    b_time TIME NOT NULL,
    e_time TIME NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

CREATE TABLE IF NOT EXISTS attendance_plan_ref_classes (
    id BIGSERIAL PRIMARY KEY,
    class_id BIGINT NOT NULL,
    plan_id BIGINT NOT NULL,
    period_no INTEGER,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

CREATE TABLE IF NOT EXISTS attendance_employee_ref_scheme_classes (
    id BIGSERIAL PRIMARY KEY,
    employee_id BIGINT NOT NULL,
    class_id BIGINT NOT NULL,
    period_no INTEGER,
    eff_date DATE NOT NULL,
    exp_date DATE,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

CREATE TABLE IF NOT EXISTS attendance_records (
    id BIGSERIAL PRIMARY KEY,
    employee_id BIGINT NOT NULL,
    period_no INTEGER DEFAULT 0,
    s_date DATE NOT NULL,
    b_att_time TIME,
    e_att_time TIME,
    b_offset INTEGER,
    e_offset INTEGER,
    b_offset_fee DECIMAL(10, 2),
    e_offset_fee DECIMAL(10, 2),
    b_time TIME,
    e_time TIME,
    day_type_id BIGINT,
    plan_id BIGINT,
    class_id BIGINT,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

CREATE TABLE IF NOT EXISTS attendance_event_declareds (
    id BIGSERIAL PRIMARY KEY,
    event_description VARCHAR(500),
    fee DECIMAL(10, 2),
    memo VARCHAR(500),
    check_man VARCHAR(50),
    manager VARCHAR(50),
    record_id BIGINT NOT NULL,
    is_begin_time BOOLEAN DEFAULT FALSE,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

CREATE TABLE IF NOT EXISTS attendance_holidays (
    id BIGSERIAL PRIMARY KEY,
    i_year INTEGER NOT NULL,
    s_date DATE NOT NULL,
    s_name VARCHAR(100),
    b_time TIME,
    e_time TIME,
    s_description VARCHAR(500),
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

CREATE TABLE IF NOT EXISTS attendance_fee_calculators (
    id BIGSERIAL PRIMARY KEY,
    day_type_id BIGINT NOT NULL,
    range_a INTEGER NOT NULL,
    range_b INTEGER NOT NULL,
    range_price DECIMAL(10, 2) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

-- 考勤报表视图
CREATE OR REPLACE VIEW v_attendance_report AS
SELECT 
    r.id AS record_id,
    r.employee_id,
    ae.employee_name,
    r.s_date,
    pt.description,
    r.b_att_time,
    r.b_offset,
    r.e_att_time,
    r.e_offset,
    r.b_offset_fee,
    r.e_offset_fee,
    (SELECT string_agg(
        CASE WHEN ed.is_begin_time THEN '早:' ELSE '晚:' END || ed.event_description,
        ';' ORDER BY ed.is_begin_time
    ) FROM attendance_event_declareds ed WHERE ed.record_id = r.id AND ed.is_deleted = FALSE) AS event,
    (SELECT MAX(ed.fee) FROM attendance_event_declareds ed WHERE ed.record_id = r.id AND ed.fee > 0 AND ed.is_deleted = FALSE) AS fee
FROM attendance_records r
LEFT JOIN attendance_plan_times pt ON r.plan_id = pt.id
LEFT JOIN attendance_employees ae ON r.employee_id = ae.employee_id
WHERE r.is_deleted = FALSE;

-- 索引
CREATE INDEX IF NOT EXISTS idx_attendance_records_employee_id ON attendance_records(employee_id);
CREATE INDEX IF NOT EXISTS idx_attendance_records_s_date ON attendance_records(s_date);
CREATE INDEX IF NOT EXISTS idx_attendance_records_employee_date ON attendance_records(employee_id, s_date);
CREATE INDEX IF NOT EXISTS idx_attendance_event_declareds_record_id ON attendance_event_declareds(record_id);
CREATE INDEX IF NOT EXISTS idx_attendance_holidays_s_date ON attendance_holidays(s_date);
CREATE INDEX IF NOT EXISTS idx_attendance_holidays_year ON attendance_holidays(i_year);