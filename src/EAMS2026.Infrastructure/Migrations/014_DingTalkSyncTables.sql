-- 钉钉数据同步表
-- 用于存储从钉钉API同步过来的原始数据

-- 1. 钉钉员工信息缓存表
CREATE TABLE IF NOT EXISTS attendance_dingtalk_employees (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(100) NOT NULL UNIQUE,
    user_name VARCHAR(100) NOT NULL,
    department_id BIGINT,
    department_name VARCHAR(200),
    status INTEGER DEFAULT 1,
    created_at TIMESTAMP DEFAULT NOW(),
    synced_at TIMESTAMP DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE,
    created_by BIGINT,
    updated_at TIMESTAMP DEFAULT NOW(),
    updated_by BIGINT
);

COMMENT ON TABLE attendance_dingtalk_employees IS '钉钉员工信息同步缓存表';
COMMENT ON COLUMN attendance_dingtalk_employees.user_id IS '钉钉用户ID';
COMMENT ON COLUMN attendance_dingtalk_employees.user_name IS '用户姓名';
COMMENT ON COLUMN attendance_dingtalk_employees.department_id IS '钉钉部门ID';
COMMENT ON COLUMN attendance_dingtalk_employees.department_name IS '部门名称';
COMMENT ON COLUMN attendance_dingtalk_employees.status IS '状态 1=在职 0=离职';
COMMENT ON COLUMN attendance_dingtalk_employees.synced_at IS '最后同步时间';

-- 2. 钉钉考勤打卡记录缓存表
CREATE TABLE IF NOT EXISTS attendance_dingtalk_card_records (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(100) NOT NULL,
    user_name VARCHAR(100),
    work_date DATE NOT NULL,
    check_time TIMESTAMP NOT NULL,
    check_type VARCHAR(20),
    time_result VARCHAR(20),
    base_check_time TIMESTAMP,
    source_type VARCHAR(10) DEFAULT 'dingtalk',
    created_at TIMESTAMP DEFAULT NOW(),
    synced_at TIMESTAMP DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE
);

COMMENT ON TABLE attendance_dingtalk_card_records IS '钉钉考勤打卡记录缓存表';
COMMENT ON COLUMN attendance_dingtalk_card_records.user_id IS '钉钉用户ID';
COMMENT ON COLUMN attendance_dingtalk_card_records.user_name IS '用户姓名';
COMMENT ON COLUMN attendance_dingtalk_card_records.work_date IS '考勤日期';
COMMENT ON COLUMN attendance_dingtalk_card_records.check_time IS '实际打卡时间';
COMMENT ON COLUMN attendance_dingtalk_card_records.check_type IS '打卡类型: OnDuty=上班 OffDuty=下班';
COMMENT ON COLUMN attendance_dingtalk_card_records.time_result IS '打卡结果: Normal=正常 Early=早退 Late=迟到 NotSigned=未打卡';
COMMENT ON COLUMN attendance_dingtalk_card_records.base_check_time IS '基线打卡时间(应打卡时间)';
COMMENT ON COLUMN attendance_dingtalk_card_records.source_type IS '数据来源';

-- 3. 钉钉同步日志表
CREATE TABLE IF NOT EXISTS dingtalk_sync_logs (
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

COMMENT ON TABLE dingtalk_sync_logs IS '钉钉数据同步日志表';
COMMENT ON COLUMN dingtalk_sync_logs.sync_type IS '同步类型: employees/card_records';
COMMENT ON COLUMN dingtalk_sync_logs.status IS '状态: running/success/failed';

-- 索引
CREATE INDEX IF NOT EXISTS idx_attendance_dingtalk_employees_user_id ON attendance_dingtalk_employees(user_id);
CREATE INDEX IF NOT EXISTS idx_attendance_dingtalk_employees_dept ON attendance_dingtalk_employees(department_id);
CREATE INDEX IF NOT EXISTS idx_attendance_dingtalk_card_records_user_id ON attendance_dingtalk_card_records(user_id);
CREATE INDEX IF NOT EXISTS idx_attendance_dingtalk_card_records_work_date ON attendance_dingtalk_card_records(work_date);
CREATE INDEX IF NOT EXISTS idx_attendance_dingtalk_card_records_sync ON attendance_dingtalk_card_records(user_id, work_date);
CREATE INDEX IF NOT EXISTS idx_dingtalk_sync_logs_type_time ON dingtalk_sync_logs(sync_type, sync_start_time);