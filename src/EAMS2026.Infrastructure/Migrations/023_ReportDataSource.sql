-- ============================================================
-- Migration 023: 报表数据源配置表
-- 说明：管理可用的数据库连接，报表可动态选择数据源
-- ============================================================

DROP TABLE IF EXISTS erp_rpt_datasource CASCADE;

CREATE TABLE erp_rpt_datasource (
    id              BIGSERIAL       PRIMARY KEY,
    name            VARCHAR(100)    NOT NULL,
    display_name    VARCHAR(200)    NOT NULL DEFAULT '',
    db_type         VARCHAR(20)     NOT NULL,       -- postgresql / sqlserver
    connection_string TEXT          NOT NULL,
    description     TEXT,
    sort_order      INT             NOT NULL DEFAULT 0,
    is_enabled      BOOLEAN         NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      BIGINT          NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      BIGINT          NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX idx_erp_rpt_datasource_name ON erp_rpt_datasource(name) WHERE is_deleted = FALSE;
CREATE INDEX idx_erp_rpt_datasource_enabled ON erp_rpt_datasource(is_enabled) WHERE is_deleted = FALSE;

COMMENT ON TABLE erp_rpt_datasource IS '报表数据源配置';
COMMENT ON COLUMN erp_rpt_datasource.name IS '数据源标识（用于报表的 query_datasource 字段）';
COMMENT ON COLUMN erp_rpt_datasource.display_name IS '显示名称';
COMMENT ON COLUMN erp_rpt_datasource.db_type IS '数据库类型：postgresql / sqlserver';
COMMENT ON COLUMN erp_rpt_datasource.connection_string IS '连接字符串';
COMMENT ON COLUMN erp_rpt_datasource.is_enabled IS '是否启用';

-- 初始化默认数据源
INSERT INTO erp_rpt_datasource (name, display_name, db_type, connection_string, description, sort_order, created_by)
VALUES
    ('main',   '主库 (PostgreSQL)',   'postgresql', 'Host=localhost;Port=5432;Database=eams2026;Username=postgres;Password=postgres', 'EAMS2026 主数据库', 1, 1),
    ('erp',    'ERP (SQL Server)',    'sqlserver',  'Server=db;Database=AppData;User ID=sa;Password=dfl_DB;TrustServerCertificate=True', 'EAMS_4.6 ERP 数据库', 2, 1);