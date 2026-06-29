-- ============================================================
-- Migration 042: 权限数据按模块层次重组
-- ============================================================

DO $$
DECLARE
    v_erp_id CONSTANT BIGINT := 38;
    v_report_id CONSTANT BIGINT := 28;
    v_erp_vouchmodify_id BIGINT;
    v_erp_settings_id BIGINT;
BEGIN
    -- 1. 新增中间层菜单节点
    INSERT INTO sys_permissions (code, name, type, parent_id, sort_order)
    VALUES ('erp-vouchmodify', '单据修改', 'menu', v_erp_id, 20)
    RETURNING id INTO v_erp_vouchmodify_id;

    INSERT INTO sys_permissions (code, name, type, parent_id, sort_order)
    VALUES ('erp-settings', '设置', 'menu', v_erp_id, 30)
    RETURNING id INTO v_erp_settings_id;

    RAISE NOTICE 'Created erp-vouchmodify (ID: %), erp-settings (ID: %)', v_erp_vouchmodify_id, v_erp_settings_id;

    -- 2. report → erp-report，挂到 erp 下
    UPDATE sys_permissions SET
        code = 'erp-report',
        name = '报表管理',
        parent_id = v_erp_id,
        sort_order = 10
    WHERE id = v_report_id;

    -- 3. 更新子权限编码和父级引用
    -- report:* → erp-report:*（父级仍指向 ID 28）
    UPDATE sys_permissions SET code = replace(code, 'report:', 'erp-report:')
    WHERE parent_id = v_report_id AND code LIKE 'report:%';

    -- vouch-modify:* → erp-vouchmodify:*（父级指向新节点）
    UPDATE sys_permissions SET
        code = replace(code, 'vouch-modify:', 'erp-vouchmodify:'),
        parent_id = v_erp_vouchmodify_id
    WHERE parent_id = v_erp_id AND code LIKE 'vouch-modify:%';

    -- erp-settings:*（子权限父级指向新 erp-settings 节点）
    UPDATE sys_permissions SET parent_id = v_erp_settings_id
    WHERE parent_id = v_erp_id AND code LIKE 'erp-settings:%';
END $$;