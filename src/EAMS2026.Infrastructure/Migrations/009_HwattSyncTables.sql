-- HWATT数据同步表
-- 用于存储从SQL Server HWATT数据库同步过来的原始数据

-- 1. HWATT员工信息表
CREATE TABLE IF NOT EXISTS attendance_hwatt_employees (
    id BIGSERIAL PRIMARY KEY,
    employee_id BIGINT NOT NULL UNIQUE,
    employee_name VARCHAR(100) NOT NULL,
    status INTEGER DEFAULT 1,
    created_at TIMESTAMP DEFAULT NOW(),
    synced_at TIMESTAMP DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE,
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

COMMENT ON TABLE attendance_hwatt_employees IS 'HWATT员工信息同步表';
COMMENT ON COLUMN attendance_hwatt_employees.employee_id IS 'HWATT系统员工ID';
COMMENT ON COLUMN attendance_hwatt_employees.employee_name IS '员工姓名';
COMMENT ON COLUMN attendance_hwatt_employees.status IS '状态 1=在职 0=离职';
COMMENT ON COLUMN attendance_hwatt_employees.synced_at IS '最后同步时间';

-- 2. HWATT考勤打卡记录表
CREATE TABLE IF NOT EXISTS attendance_hwatt_card_records (
    id BIGSERIAL PRIMARY KEY,
    employee_id BIGINT NOT NULL,
    card_time TIMESTAMP NOT NULL,
    card_date DATE GENERATED ALWAYS AS (CAST(card_time AS DATE)) STORED,
    card_type_id INTEGER,
    dev_id BIGINT,
    card_begin TIME,
    card_end TIME,
    created_at TIMESTAMP DEFAULT NOW(),
    synced_at TIMESTAMP DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE
);

COMMENT ON TABLE attendance_hwatt_card_records IS 'HWATT考勤打卡记录同步表';
COMMENT ON COLUMN attendance_hwatt_card_records.employee_id IS 'HWATT系统员工ID';
COMMENT ON COLUMN attendance_hwatt_card_records.card_time IS '打卡时间';
COMMENT ON COLUMN attendance_hwatt_card_records.card_date IS '打卡日期(计算列)';
COMMENT ON COLUMN attendance_hwatt_card_records.card_type_id IS '打卡类型ID';
COMMENT ON COLUMN attendance_hwatt_card_records.dev_id IS '设备ID';
COMMENT ON COLUMN attendance_hwatt_card_records.card_begin IS '上班打卡时间';
COMMENT ON COLUMN attendance_hwatt_card_records.card_end IS '下班打卡时间';

-- 3. 同步日志表
CREATE TABLE IF NOT EXISTS hwatt_sync_logs (
    id BIGSERIAL PRIMARY KEY,
    sync_type VARCHAR(50) NOT NULL,
    sync_start_time TIMESTAMP NOT NULL,
    sync_end_time TIMESTAMP,
    status VARCHAR(20) DEFAULT 'running',
    total_count INTEGER DEFAULT 0,
    success_count INTEGER DEFAULT 0,
    error_message TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

COMMENT ON TABLE hwatt_sync_logs IS 'HWATT数据同步日志表';
COMMENT ON COLUMN hwatt_sync_logs.sync_type IS '同步类型: employees/card_records';
COMMENT ON COLUMN hwatt_sync_logs.status IS '状态: running/success/failed';

-- 索引
CREATE INDEX IF NOT EXISTS idx_attendance_hwatt_employees_employee_id ON attendance_hwatt_employees(employee_id);
CREATE INDEX IF NOT EXISTS idx_attendance_hwatt_card_records_employee_id ON attendance_hwatt_card_records(employee_id);
CREATE INDEX IF NOT EXISTS idx_attendance_hwatt_card_records_card_date ON attendance_hwatt_card_records(card_date);
CREATE INDEX IF NOT EXISTS idx_attendance_hwatt_card_records_sync ON attendance_hwatt_card_records(employee_id, card_date);
CREATE INDEX IF NOT EXISTS idx_hwatt_sync_logs_type_time ON hwatt_sync_logs(sync_type, sync_start_time);
