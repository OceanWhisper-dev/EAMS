-- 数据权限规则表
-- 用于配置各角色在不同模块下的数据访问范围
-- 例如：考勤管理模块，系统管理员可查看全部，部门主管仅可查看本部门

CREATE TABLE IF NOT EXISTS data_permission_rules (
    id BIGSERIAL PRIMARY KEY,
    role_id BIGINT NOT NULL REFERENCES roles(id),
    module VARCHAR(50) NOT NULL,
    data_scope VARCHAR(20) NOT NULL DEFAULT 'DEPARTMENT',
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by BIGINT NOT NULL DEFAULT 0,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by BIGINT NOT NULL DEFAULT 0
);

-- 唯一约束：一个角色在一个模块下只能有一条规则
CREATE UNIQUE INDEX IF NOT EXISTS idx_data_permission_rules_unique
    ON data_permission_rules(role_id, module) WHERE is_deleted = FALSE;

COMMENT ON TABLE data_permission_rules IS '数据权限规则表';
COMMENT ON COLUMN data_permission_rules.role_id IS '角色ID';
COMMENT ON COLUMN data_permission_rules.module IS '模块标识(如 attendance)';
COMMENT ON COLUMN data_permission_rules.data_scope IS '数据范围: ALL=全部数据, DEPARTMENT=本部门数据';