-- ============================================================
-- EAMS2026 基础域 DDL
-- 目标数据库: PostgreSQL 18
-- 说明: 基础域10张表 + 索引 + 初始数据
-- ============================================================

-- 创建数据库（需在psql中以superuser执行）
-- CREATE DATABASE eams2026 WITH ENCODING 'UTF8' LC_COLLATE 'zh_CN.UTF-8' LC_CTYPE 'zh_CN.UTF-8';

-- ============================================================
-- 1. 部门表 (sys_departments)
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_departments (
    id              BIGSERIAL       PRIMARY KEY,
    name            VARCHAR(100)    NOT NULL,
    code            VARCHAR(50)     NOT NULL,
    parent_id       BIGINT          REFERENCES sys_departments(id),
    sort_order      INT             NOT NULL DEFAULT 0,
    status          BOOLEAN         NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      BIGINT          NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      BIGINT          NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_departments_code ON sys_departments(code) WHERE is_deleted = FALSE;
CREATE INDEX IF NOT EXISTS idx_sys_departments_parent_id ON sys_departments(parent_id);

COMMENT ON TABLE sys_departments IS '部门表';
COMMENT ON COLUMN sys_departments.name IS '部门名称';
COMMENT ON COLUMN sys_departments.code IS '部门编码';
COMMENT ON COLUMN sys_departments.parent_id IS '上级部门ID';

-- ============================================================
-- 2. 员工档案表 (sys_employees)
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_employees (
    id              BIGSERIAL       PRIMARY KEY,
    employee_no     VARCHAR(50)     NOT NULL,
    name            VARCHAR(100)    NOT NULL,
    gender          VARCHAR(10),
    phone           VARCHAR(20),
    email           VARCHAR(200),
    department_id   BIGINT          REFERENCES sys_departments(id),
    position        VARCHAR(100),
    hire_date       DATE,
    status          BOOLEAN         NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      BIGINT          NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      BIGINT          NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_employees_no ON sys_employees(employee_no) WHERE is_deleted = FALSE;
CREATE INDEX IF NOT EXISTS idx_sys_employees_department_id ON sys_employees(department_id);
CREATE INDEX IF NOT EXISTS idx_sys_employees_name ON sys_employees(name);

COMMENT ON TABLE sys_employees IS '员工档案表';
COMMENT ON COLUMN sys_employees.employee_no IS '工号';
COMMENT ON COLUMN sys_employees.name IS '姓名';
COMMENT ON COLUMN sys_employees.department_id IS '所属部门ID';
COMMENT ON COLUMN sys_employees.position IS '职位';

-- ============================================================
-- 3. 用户表 (sys_users)
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_users (
    id              BIGSERIAL       PRIMARY KEY,
    username        VARCHAR(100)    NOT NULL,
    password_hash   VARCHAR(500)    NOT NULL,
    employee_id     BIGINT          REFERENCES sys_employees(id),
    status          BOOLEAN         NOT NULL DEFAULT TRUE,
    last_login_at   TIMESTAMPTZ,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      BIGINT          NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      BIGINT          NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_users_username ON sys_users(username) WHERE is_deleted = FALSE;
CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_users_employee_id ON sys_users(employee_id) WHERE is_deleted = FALSE;

COMMENT ON TABLE sys_users IS '用户表（登录账号）';
COMMENT ON COLUMN sys_users.username IS '用户名';
COMMENT ON COLUMN sys_users.password_hash IS 'BCrypt密码哈希';
COMMENT ON COLUMN sys_users.employee_id IS '关联员工ID';
COMMENT ON COLUMN sys_users.last_login_at IS '最后登录时间';

-- ============================================================
-- 4. 角色表 (sys_roles)
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_roles (
    id              BIGSERIAL       PRIMARY KEY,
    name            VARCHAR(100)    NOT NULL,
    code            VARCHAR(50)     NOT NULL,
    description     VARCHAR(500),
    status          BOOLEAN         NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      BIGINT          NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      BIGINT          NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_roles_code ON sys_roles(code) WHERE is_deleted = FALSE;

COMMENT ON TABLE sys_roles IS '角色表';
COMMENT ON COLUMN sys_roles.name IS '角色名称';
COMMENT ON COLUMN sys_roles.code IS '角色编码';
COMMENT ON COLUMN sys_roles.description IS '角色描述';

-- ============================================================
-- 5. 用户角色关联表 (sys_user_roles)
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_user_roles (
    id              BIGSERIAL       PRIMARY KEY,
    user_id         BIGINT          NOT NULL REFERENCES sys_users(id) ON DELETE CASCADE,
    role_id         BIGINT          NOT NULL REFERENCES sys_roles(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_user_roles_unique ON sys_user_roles(user_id, role_id);
CREATE INDEX IF NOT EXISTS idx_sys_user_roles_role_id ON sys_user_roles(role_id);

COMMENT ON TABLE sys_user_roles IS '用户角色关联表';

-- ============================================================
-- 6. 权限表 (sys_permissions)
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_permissions (
    id              BIGSERIAL       PRIMARY KEY,
    name            VARCHAR(100)    NOT NULL,
    code            VARCHAR(100)    NOT NULL,
    type            VARCHAR(20)     NOT NULL DEFAULT 'menu',
    parent_id       BIGINT          REFERENCES sys_permissions(id),
    path            VARCHAR(500),
    icon            VARCHAR(100),
    sort_order      INT             NOT NULL DEFAULT 0,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      BIGINT          NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      BIGINT          NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_permissions_code ON sys_permissions(code) WHERE is_deleted = FALSE;
CREATE INDEX IF NOT EXISTS idx_sys_permissions_parent_id ON sys_permissions(parent_id);

COMMENT ON TABLE sys_permissions IS '权限表（菜单/按钮/API）';
COMMENT ON COLUMN sys_permissions.name IS '权限名称';
COMMENT ON COLUMN sys_permissions.code IS '权限编码（如 system:user:create）';
COMMENT ON COLUMN sys_permissions.type IS '权限类型: menu(菜单)/button(按钮)/api(接口)';
COMMENT ON COLUMN sys_permissions.path IS '前端路由路径';
COMMENT ON COLUMN sys_permissions.icon IS '菜单图标';

-- ============================================================
-- 7. 角色权限关联表 (sys_role_permissions)
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_role_permissions (
    id              BIGSERIAL       PRIMARY KEY,
    role_id         BIGINT          NOT NULL REFERENCES sys_roles(id) ON DELETE CASCADE,
    permission_id   BIGINT          NOT NULL REFERENCES sys_permissions(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_role_permissions_unique ON sys_role_permissions(role_id, permission_id);
CREATE INDEX IF NOT EXISTS idx_sys_role_permissions_permission_id ON sys_role_permissions(permission_id);

COMMENT ON TABLE sys_role_permissions IS '角色权限关联表';

-- ============================================================
-- 8. 操作日志表 (sys_operation_logs)
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_operation_logs (
    id              BIGSERIAL       PRIMARY KEY,
    user_id         BIGINT,
    username        VARCHAR(100),
    operation_type  VARCHAR(20)     NOT NULL,
    module          VARCHAR(50)     NOT NULL,
    entity_type     VARCHAR(100),
    entity_id       BIGINT,
    description     TEXT,
    old_value       JSONB,
    new_value       JSONB,
    ip_address      VARCHAR(50),
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_sys_operation_logs_user_id ON sys_operation_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_sys_operation_logs_module ON sys_operation_logs(module);
CREATE INDEX IF NOT EXISTS idx_sys_operation_logs_created_at ON sys_operation_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_sys_operation_logs_entity ON sys_operation_logs(entity_type, entity_id);

COMMENT ON TABLE sys_operation_logs IS '操作日志表';
COMMENT ON COLUMN sys_operation_logs.operation_type IS '操作类型: create/update/delete';
COMMENT ON COLUMN sys_operation_logs.module IS '所属模块';
COMMENT ON COLUMN sys_operation_logs.entity_type IS '操作实体类型';
COMMENT ON COLUMN sys_operation_logs.entity_id IS '操作实体ID';
COMMENT ON COLUMN sys_operation_logs.old_value IS '操作前数据(JSON)';
COMMENT ON COLUMN sys_operation_logs.new_value IS '操作后数据(JSON)';

-- ============================================================
-- 9. 字典类型表 (sys_dict_types)
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_dict_types (
    id              BIGSERIAL       PRIMARY KEY,
    name            VARCHAR(100)    NOT NULL,
    code            VARCHAR(50)     NOT NULL,
    description     VARCHAR(500),
    status          BOOLEAN         NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      BIGINT          NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      BIGINT          NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_sys_dict_types_code ON sys_dict_types(code) WHERE is_deleted = FALSE;

COMMENT ON TABLE sys_dict_types IS '字典类型表';
COMMENT ON COLUMN sys_dict_types.name IS '字典类型名称（如：性别）';
COMMENT ON COLUMN sys_dict_types.code IS '字典类型编码（如：gender）';

-- ============================================================
-- 10. 字典项表 (sys_dict_items)
-- ============================================================
CREATE TABLE IF NOT EXISTS sys_dict_items (
    id              BIGSERIAL       PRIMARY KEY,
    dict_type_id    BIGINT          NOT NULL REFERENCES sys_dict_types(id) ON DELETE CASCADE,
    label           VARCHAR(100)    NOT NULL,
    value           VARCHAR(100)    NOT NULL,
    sort_order      INT             NOT NULL DEFAULT 0,
    status          BOOLEAN         NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      BIGINT          NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      BIGINT          NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_sys_dict_items_dict_type_id ON sys_dict_items(dict_type_id);
CREATE INDEX IF NOT EXISTS idx_sys_dict_items_sort ON sys_dict_items(dict_type_id, sort_order);

COMMENT ON TABLE sys_dict_items IS '字典项表';
COMMENT ON COLUMN sys_dict_items.label IS '显示值';
COMMENT ON COLUMN sys_dict_items.value IS '存储值';

-- ============================================================
-- 初始数据
-- ============================================================

-- 管理员角色
INSERT INTO sys_roles (id, name, code, description, created_by, updated_by)
VALUES (1, '超级管理员', 'super_admin', '系统超级管理员，拥有所有权限', 1, 1);

-- 管理员部门
INSERT INTO sys_departments (id, name, code, created_by, updated_by)
VALUES (1, '总公司', 'head_office', 1, 1);

-- 管理员员工
INSERT INTO sys_employees (id, employee_no, name, department_id, position, created_by, updated_by)
VALUES (1, 'ADMIN', '系统管理员', 1, '系统管理员', 1, 1);

-- 管理员用户 (密码: admin123)
INSERT INTO sys_users (id, username, password_hash, employee_id, created_by, updated_by)
VALUES (1, 'admin', '$2a$11$RvHGzGOXKmEJ968isQWKSuleBGV16e.tr2iwi2.P4Md4oTaoRBtvW', 1, 1, 1);

-- 用户角色关联
INSERT INTO sys_user_roles (user_id, role_id) VALUES (1, 1);

-- 更新自引用
UPDATE sys_departments SET created_by = 1, updated_by = 1 WHERE id = 1;
UPDATE sys_employees SET created_by = 1, updated_by = 1 WHERE id = 1;
UPDATE sys_users SET created_by = 1, updated_by = 1 WHERE id = 1;

-- ============================================================
-- 索引维护
-- ============================================================
VACUUM ANALYZE;