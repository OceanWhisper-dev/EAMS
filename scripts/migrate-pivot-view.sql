-- Pivot Table View Configuration Migration
-- 用于保存用户对报表透视表的配置（行列、聚合、过滤设置）
BEGIN;

-- 创建 rpt_pivot_view 表
CREATE TABLE IF NOT EXISTS rpt_pivot_view (
    autoid      BIGSERIAL PRIMARY KEY,
    report_id   BIGINT NOT NULL REFERENCES rpt_report(id) ON DELETE CASCADE,
    user_id     BIGINT NOT NULL DEFAULT -999,  -- -999 表示系统缺省配置
    pivot_name  VARCHAR(200) NOT NULL DEFAULT '默认格式',
    pivot_params TEXT NOT NULL DEFAULT '{}',   -- 存储 pivottable 的 JSON 配置
    is_last     BOOLEAN NOT NULL DEFAULT FALSE, -- 是否是上次使用的格式
    created_at  TIMESTAMP DEFAULT NOW(),
    updated_at  TIMESTAMP DEFAULT NOW()
);

-- 索引：加速按报表+用户查询
CREATE INDEX IF NOT EXISTS idx_pivot_view_report_user ON rpt_pivot_view(report_id, user_id);

COMMIT;