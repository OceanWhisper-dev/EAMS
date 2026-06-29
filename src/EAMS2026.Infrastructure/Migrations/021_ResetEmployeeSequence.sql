-- ============================================================
-- 021_ResetEmployeeSequence.sql
-- 修复employees表的BIGSERIAL序列与当前最大ID不同步的问题
-- 
-- 原因：迁移脚本001和013通过显式指定id值INSERT员工数据，
-- 导致序列从未被更新，nextval可能生成与现有数据冲突的ID。
-- ============================================================

-- 将employees_id_seq序列重置为当前最大ID+1
SELECT SETVAL('employees_id_seq', (SELECT COALESCE(MAX(id), 0) + 1 FROM employees));