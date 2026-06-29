-- 修复打卡记录重复数据问题
-- 1. hwatt_card_records 表：清理重复数据并添加唯一约束
-- 2. dingtalk_card_records 表：清理重复数据并添加唯一约束

-- ========== HWATT打卡记录 ==========

-- 删除重复记录，只保留每组 (employee_id, card_time) 中 id 最小的那条
DELETE FROM hwatt_card_records
WHERE id NOT IN (
    SELECT MIN(id)
    FROM hwatt_card_records
    GROUP BY employee_id, card_time
);

-- 添加唯一约束，防止后续插入重复数据
ALTER TABLE hwatt_card_records
ADD CONSTRAINT uq_hwatt_card_records_employee_time
UNIQUE (employee_id, card_time);

COMMENT ON CONSTRAINT uq_hwatt_card_records_employee_time ON hwatt_card_records
IS '同一员工同一时间只能有一条打卡记录';

-- ========== 钉钉打卡记录 ==========

-- 删除重复记录，只保留每组 (user_id, check_time) 中 id 最小的那条
DELETE FROM dingtalk_card_records
WHERE id NOT IN (
    SELECT MIN(id)
    FROM dingtalk_card_records
    GROUP BY user_id, check_time
);

-- 添加唯一约束
ALTER TABLE dingtalk_card_records
ADD CONSTRAINT uq_dingtalk_card_records_user_time
UNIQUE (user_id, check_time);

COMMENT ON CONSTRAINT uq_dingtalk_card_records_user_time ON dingtalk_card_records
IS '同一用户同一时间只能有一条打卡记录';