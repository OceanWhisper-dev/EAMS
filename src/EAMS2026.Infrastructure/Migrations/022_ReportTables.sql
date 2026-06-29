-- ============================================================
-- Migration 022: 报表模块（Report）建表
-- 说明：报表自定义设计、执行、权限、收藏、日志
-- 前缀：erp_rpt_
-- ============================================================

-- 0. 清理旧表（幂等）
DROP TABLE IF EXISTS erp_rpt_execution_log CASCADE;
DROP TABLE IF EXISTS erp_rpt_bookmark CASCADE;
DROP TABLE IF EXISTS erp_rpt_field_permission CASCADE;
DROP TABLE IF EXISTS erp_rpt_permission CASCADE;
DROP TABLE IF EXISTS erp_rpt_chart CASCADE;
DROP TABLE IF EXISTS erp_rpt_sort CASCADE;
DROP TABLE IF EXISTS erp_rpt_filter CASCADE;
DROP TABLE IF EXISTS erp_rpt_field CASCADE;
DROP TABLE IF EXISTS erp_rpt_report CASCADE;
DROP TABLE IF EXISTS erp_rpt_category CASCADE;

-- ============================================================
-- 1. 报表分类表
-- ============================================================
CREATE TABLE erp_rpt_category (
    id              BIGSERIAL       PRIMARY KEY,
    name            VARCHAR(100)    NOT NULL,
    parent_id       BIGINT          REFERENCES erp_rpt_category(id) ON DELETE SET NULL,
    sort_order      INT             NOT NULL DEFAULT 0,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      BIGINT          NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      BIGINT          NOT NULL DEFAULT 0
);

CREATE INDEX idx_erp_rpt_category_parent ON erp_rpt_category(parent_id) WHERE is_deleted = FALSE;

-- ============================================================
-- 2. 报表主表
-- ============================================================
CREATE TABLE erp_rpt_report (
    id                  BIGSERIAL       PRIMARY KEY,
    name                VARCHAR(200)    NOT NULL,
    title               VARCHAR(500)    NOT NULL DEFAULT '',
    description         TEXT,
    category_id         BIGINT          REFERENCES erp_rpt_category(id) ON DELETE SET NULL,
    query_type          VARCHAR(20)     NOT NULL DEFAULT 'sql',  -- sql / proc / table
    query_text          TEXT            NOT NULL DEFAULT '',
    query_datasource    VARCHAR(50)     NOT NULL DEFAULT 'main', -- main / erp
    is_system           BOOLEAN         NOT NULL DEFAULT FALSE,
    status              VARCHAR(20)     NOT NULL DEFAULT 'draft', -- draft / published / disabled
    is_deleted          BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by          BIGINT          NOT NULL DEFAULT 0,
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by          BIGINT          NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX idx_erp_rpt_report_name ON erp_rpt_report(name) WHERE is_deleted = FALSE;
CREATE INDEX idx_erp_rpt_report_category ON erp_rpt_report(category_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_erp_rpt_report_status ON erp_rpt_report(status) WHERE is_deleted = FALSE;

-- ============================================================
-- 3. 报表字段表
-- ============================================================
CREATE TABLE erp_rpt_field (
    id              BIGSERIAL       PRIMARY KEY,
    report_id       BIGINT          NOT NULL REFERENCES erp_rpt_report(id) ON DELETE CASCADE,
    field_name      VARCHAR(200)    NOT NULL,
    field_title     VARCHAR(200)    NOT NULL DEFAULT '',
    field_type      VARCHAR(50)     NOT NULL DEFAULT 'string',  -- string/number/date/boolean/money
    sort_order      INT             NOT NULL DEFAULT 0,
    width           INT             NOT NULL DEFAULT 0,
    align           VARCHAR(10)     NOT NULL DEFAULT 'left',    -- left/center/right
    is_display      BOOLEAN         NOT NULL DEFAULT TRUE,
    is_sortable     BOOLEAN         NOT NULL DEFAULT TRUE,
    is_filterable   BOOLEAN         NOT NULL DEFAULT FALSE,
    is_groupable    BOOLEAN         NOT NULL DEFAULT FALSE,
    is_summary      BOOLEAN         NOT NULL DEFAULT FALSE,
    summary_type    VARCHAR(20),              -- sum/avg/count/max/min
    format_pattern  VARCHAR(100),             -- e.g. #,##0.00
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_erp_rpt_field_report ON erp_rpt_field(report_id) WHERE is_deleted = FALSE;

-- ============================================================
-- 4. 报表过滤条件表
-- ============================================================
CREATE TABLE erp_rpt_filter (
    id              BIGSERIAL       PRIMARY KEY,
    report_id       BIGINT          NOT NULL REFERENCES erp_rpt_report(id) ON DELETE CASCADE,
    field_name      VARCHAR(200)    NOT NULL,
    label           VARCHAR(200)    NOT NULL DEFAULT '',
    operator        VARCHAR(20)     NOT NULL DEFAULT 'eq',  -- eq/ne/gt/ge/lt/le/like/in/between
    default_value   TEXT,
    control_type    VARCHAR(30)     NOT NULL DEFAULT 'text',  -- text/select/date/daterange/number/checkbox
    options_query   TEXT,           -- 下拉框选项SQL（仅select类型）
    sort_order      INT             NOT NULL DEFAULT 0,
    is_required     BOOLEAN         NOT NULL DEFAULT FALSE,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_erp_rpt_filter_report ON erp_rpt_filter(report_id) WHERE is_deleted = FALSE;

-- ============================================================
-- 5. 报表默认排序配置表
-- ============================================================
CREATE TABLE erp_rpt_sort (
    id              BIGSERIAL       PRIMARY KEY,
    report_id       BIGINT          NOT NULL REFERENCES erp_rpt_report(id) ON DELETE CASCADE,
    field_name      VARCHAR(200)    NOT NULL,
    direction       VARCHAR(4)      NOT NULL DEFAULT 'asc',  -- asc / desc
    sort_order      INT             NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_erp_rpt_sort_report ON erp_rpt_sort(report_id);

-- ============================================================
-- 6. 报表图表配置表
-- ============================================================
CREATE TABLE erp_rpt_chart (
    id              BIGSERIAL       PRIMARY KEY,
    report_id       BIGINT          NOT NULL REFERENCES erp_rpt_report(id) ON DELETE CASCADE,
    chart_type      VARCHAR(20)     NOT NULL DEFAULT 'bar',  -- bar/line/pie/scatter/radar
    title           VARCHAR(200)    NOT NULL DEFAULT '',
    x_field         VARCHAR(200)    NOT NULL DEFAULT '',
    y_fields        JSONB           NOT NULL DEFAULT '[]',  -- ["field1","field2"]
    group_field     VARCHAR(200),
    options         JSONB,          -- 扩展配置（颜色、堆叠等）
    sort_order      INT             NOT NULL DEFAULT 0,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_erp_rpt_chart_report ON erp_rpt_chart(report_id) WHERE is_deleted = FALSE;

-- ============================================================
-- 7. 报表权限表
-- ============================================================
CREATE TABLE erp_rpt_permission (
    id              BIGSERIAL       PRIMARY KEY,
    report_id       BIGINT          NOT NULL REFERENCES erp_rpt_report(id) ON DELETE CASCADE,
    principal_type  VARCHAR(10)     NOT NULL,  -- role / user
    principal_id    BIGINT          NOT NULL,
    access_type     VARCHAR(20)     NOT NULL,  -- view / export / manage
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_erp_rpt_permission_uniq 
    ON erp_rpt_permission(report_id, principal_type, principal_id, access_type);

-- ============================================================
-- 8. 报表字段级权限表
-- ============================================================
CREATE TABLE erp_rpt_field_permission (
    id              BIGSERIAL       PRIMARY KEY,
    report_id       BIGINT          NOT NULL REFERENCES erp_rpt_report(id) ON DELETE CASCADE,
    field_name      VARCHAR(200)    NOT NULL,
    principal_type  VARCHAR(10)     NOT NULL,  -- role / user
    principal_id    BIGINT          NOT NULL,
    is_visible      BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_erp_rpt_field_permission_uniq 
    ON erp_rpt_field_permission(report_id, field_name, principal_type, principal_id);

-- ============================================================
-- 9. 用户收藏表
-- ============================================================
CREATE TABLE erp_rpt_bookmark (
    id              BIGSERIAL       PRIMARY KEY,
    user_id         BIGINT          NOT NULL,
    report_id       BIGINT          NOT NULL REFERENCES erp_rpt_report(id) ON DELETE CASCADE,
    sort_order      INT             NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_erp_rpt_bookmark_uniq ON erp_rpt_bookmark(user_id, report_id);
CREATE INDEX idx_erp_rpt_bookmark_user ON erp_rpt_bookmark(user_id);

-- ============================================================
-- 10. 报表执行日志表
-- ============================================================
CREATE TABLE erp_rpt_execution_log (
    id              BIGSERIAL       PRIMARY KEY,
    report_id       BIGINT          NOT NULL REFERENCES erp_rpt_report(id) ON DELETE CASCADE,
    user_id         BIGINT          NOT NULL,
    params          JSONB,
    row_count       INT             NOT NULL DEFAULT 0,
    duration_ms     INT             NOT NULL DEFAULT 0,
    is_success      BOOLEAN         NOT NULL DEFAULT TRUE,
    error_message   TEXT,
    executed_at     TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_erp_rpt_execution_log_report ON erp_rpt_execution_log(report_id);
CREATE INDEX idx_erp_rpt_execution_log_user ON erp_rpt_execution_log(user_id);
CREATE INDEX idx_erp_rpt_execution_log_time ON erp_rpt_execution_log(executed_at DESC);

-- ============================================================
-- 初始化：默认分类
-- ============================================================
INSERT INTO erp_rpt_category (id, name, sort_order) VALUES
    (1, '常用报表', 0),
    (2, '销售报表', 10),
    (3, '库存报表', 20),
    (4, '财务报表', 30),
    (5, '人事报表', 40),
    (6, '其他',     999);