-- ============================================================
-- Migration 025: 业务员映射表
-- 说明：将系统员工与ERP业务员代码关联，用于报表数据权限控制
-- 有映射的员工=业务员（只能看自己数据），无映射=可看全部
-- ============================================================

DROP TABLE IF EXISTS erp_rpt_salesperson_map CASCADE;

CREATE TABLE erp_rpt_salesperson_map (
    employee_id         BIGINT          NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    salesperson_code    VARCHAR(50)     NOT NULL,
    salesperson_name    VARCHAR(100),
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    PRIMARY KEY (employee_id)
);

CREATE INDEX idx_erp_rpt_salesperson_map_code ON erp_rpt_salesperson_map(salesperson_code);

COMMENT ON TABLE erp_rpt_salesperson_map IS '员工-业务员映射表';
COMMENT ON COLUMN erp_rpt_salesperson_map.employee_id IS '系统员工ID';
COMMENT ON COLUMN erp_rpt_salesperson_map.salesperson_code IS 'ERP业务员编码（对应U8 Person.cPersonCode）';
COMMENT ON COLUMN erp_rpt_salesperson_map.salesperson_name IS '业务员名称（冗余，方便查看）';