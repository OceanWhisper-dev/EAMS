-- ============================================================
-- 032_ErpPermissionAdjustment.sql
-- 菜单结构调整后，调整权限条目以匹配新的 ERP辅助 菜单
-- 变更说明：
-- 1. `erp` 菜单名称：ERP单据修改 → ERP辅助
-- 2. `report:datasource` → `erp:datasource`（数据源管理 → 数据源配置，归属 erp）
-- 3. `report:salesperson` → `erp:salesperson`（业务员映射 → 业务员对照，归属 erp）
-- ============================================================

BEGIN;

-- ===== 1. 修改 erp 顶级菜单名称 =====
UPDATE permissions
SET name = 'ERP辅助'
WHERE code = 'erp' AND is_deleted = FALSE
  AND name = 'ERP单据修改';

-- ===== 2. report:datasource → erp:datasource =====
-- 先获取目标父级 id
DO $$
DECLARE
    v_erp_id BIGINT;
    v_report_id BIGINT;
    v_max_sort BIGINT;
BEGIN
    SELECT id INTO v_erp_id FROM permissions WHERE code = 'erp' AND is_deleted = FALSE LIMIT 1;
    SELECT id INTO v_report_id FROM permissions WHERE code = 'report' AND is_deleted = FALSE LIMIT 1;

    -- 获取 erp 下当前最大排序号
    SELECT COALESCE(MAX(sort_order), 0) + 1 INTO v_max_sort
    FROM permissions WHERE parent_id = v_erp_id AND is_deleted = FALSE;

    -- 更新 report:datasource → erp:datasource
    UPDATE permissions
    SET code = 'erp:datasource'
      , name = '数据源配置'
      , parent_id = v_erp_id
      , sort_order = v_max_sort
    WHERE code = 'report:datasource' AND is_deleted = FALSE;

    -- 更新 report:salesperson → erp:salesperson
    UPDATE permissions
    SET code = 'erp:salesperson'
      , name = '业务员对照'
      , parent_id = v_erp_id
      , sort_order = v_max_sort + 1
    WHERE code = 'report:salesperson' AND is_deleted = FALSE;
END $$;

-- ===== 3. 将新权限自动授予超级管理员 =====
INSERT INTO role_permissions (role_id, permission_id)
SELECT 1, p.id
FROM permissions p
WHERE p.code IN ('erp:datasource', 'erp:salesperson')
  AND p.is_deleted = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM role_permissions rp
    WHERE rp.role_id = 1 AND rp.permission_id = p.id
  );

COMMIT;