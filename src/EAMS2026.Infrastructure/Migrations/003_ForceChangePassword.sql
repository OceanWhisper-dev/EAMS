-- ============================================================
-- EAMS2026 用户强制修改密码功能 DDL
-- ============================================================

ALTER TABLE users ADD COLUMN IF NOT EXISTS force_change_password BOOLEAN NOT NULL DEFAULT FALSE;

COMMENT ON COLUMN users.force_change_password IS '是否强制修改密码（密码重置后开启，修改密码后关闭）';