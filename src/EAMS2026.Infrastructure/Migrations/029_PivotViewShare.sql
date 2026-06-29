-- ============================================================
-- 029_PivotViewShare.sql
-- 透视表格式定向共享功能
-- ============================================================

-- 1. erp_rpt_pivot_view 增加 is_shared 标记和创建者名称
ALTER TABLE erp_rpt_pivot_view ADD COLUMN IF NOT EXISTS is_shared BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE erp_rpt_pivot_view ADD COLUMN IF NOT EXISTS created_by_name VARCHAR(100);

COMMENT ON COLUMN erp_rpt_pivot_view.is_shared IS '是否已共享（有定向分享记录）';
COMMENT ON COLUMN erp_rpt_pivot_view.created_by_name IS '创建者姓名（用于共享时显示）';

-- 2. 创建共享目标表（指定用户或角色）
CREATE TABLE IF NOT EXISTS erp_rpt_pivot_view_share (
    autoid BIGSERIAL PRIMARY KEY,
    pivot_view_id BIGINT NOT NULL REFERENCES erp_rpt_pivot_view(autoid) ON DELETE CASCADE,
    target_type VARCHAR(20) NOT NULL CHECK (target_type IN ('user', 'role')),
    target_id BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(pivot_view_id, target_type, target_id)
);

CREATE INDEX IF NOT EXISTS idx_pivot_view_share_target
    ON erp_rpt_pivot_view_share(target_type, target_id);

COMMENT ON TABLE erp_rpt_pivot_view_share IS '透视表格式定向共享目标';
COMMENT ON COLUMN erp_rpt_pivot_view_share.pivot_view_id IS '透视表配置ID';
COMMENT ON COLUMN erp_rpt_pivot_view_share.target_type IS '共享目标类型：user=指定用户，role=指定角色';
COMMENT ON COLUMN erp_rpt_pivot_view_share.target_id IS '共享目标ID（用户ID或角色ID）';