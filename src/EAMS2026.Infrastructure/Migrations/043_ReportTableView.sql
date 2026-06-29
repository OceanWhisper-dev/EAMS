-- Migration 043: 报表数据表视图格式配置（保存/分享表头样式，类似透视表格式功能）

-- ============================================================
-- 1. 数据表视图格式配置表
-- ============================================================
CREATE TABLE IF NOT EXISTS erp_rpt_table_view (
    autoid BIGSERIAL PRIMARY KEY,
    report_id BIGINT NOT NULL REFERENCES erp_rpt_report(id) ON DELETE CASCADE,
    user_id BIGINT NOT NULL DEFAULT 0,
    view_name VARCHAR(200) NOT NULL,
    view_params TEXT NOT NULL DEFAULT '{}',
    is_last BOOLEAN NOT NULL DEFAULT FALSE,
    is_shared BOOLEAN NOT NULL DEFAULT FALSE,
    created_by_name VARCHAR(100),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_rpt_table_view_report
    ON erp_rpt_table_view(report_id, user_id);

COMMENT ON TABLE erp_rpt_table_view IS '报表数据表视图格式配置（列顺序/宽度/可见性/冻结/排序等）';
COMMENT ON COLUMN erp_rpt_table_view.autoid IS '主键';
COMMENT ON COLUMN erp_rpt_table_view.report_id IS '关联报表ID';
COMMENT ON COLUMN erp_rpt_table_view.user_id IS '创建用户ID（-999为系统缺省）';
COMMENT ON COLUMN erp_rpt_table_view.view_name IS '视图名称';
COMMENT ON COLUMN erp_rpt_table_view.view_params IS '视图配置JSON（列顺序/宽度/可见性/冻结/排序等）';
COMMENT ON COLUMN erp_rpt_table_view.is_last IS '是否为最后使用的配置';
COMMENT ON COLUMN erp_rpt_table_view.is_shared IS '是否已共享给他人';
COMMENT ON COLUMN erp_rpt_table_view.created_by_name IS '创建人显示名';
COMMENT ON COLUMN erp_rpt_table_view.created_at IS '创建时间';
COMMENT ON COLUMN erp_rpt_table_view.updated_at IS '更新时间';

-- ============================================================
-- 2. 数据表视图共享目标表
-- ============================================================
CREATE TABLE IF NOT EXISTS erp_rpt_table_view_share (
    autoid BIGSERIAL PRIMARY KEY,
    view_id BIGINT NOT NULL REFERENCES erp_rpt_table_view(autoid) ON DELETE CASCADE,
    target_type VARCHAR(20) NOT NULL CHECK (target_type IN ('user', 'role')),
    target_id BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(view_id, target_type, target_id)
);

CREATE INDEX IF NOT EXISTS idx_rpt_table_view_share_target
    ON erp_rpt_table_view_share(target_type, target_id);

COMMENT ON TABLE erp_rpt_table_view_share IS '报表数据表视图共享目标';
COMMENT ON COLUMN erp_rpt_table_view_share.autoid IS '主键';
COMMENT ON COLUMN erp_rpt_table_view_share.view_id IS '关联视图配置ID';
COMMENT ON COLUMN erp_rpt_table_view_share.target_type IS '共享目标类型：user(用户)/role(角色)';
COMMENT ON COLUMN erp_rpt_table_view_share.target_id IS '共享目标ID';
COMMENT ON COLUMN erp_rpt_table_view_share.created_at IS '创建时间';
