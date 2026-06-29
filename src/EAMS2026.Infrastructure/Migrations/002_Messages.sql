-- ============================================================
-- EAMS2026 消息通知模块 DDL
-- ============================================================

-- 11. 消息表 (sys_messages)
CREATE TABLE IF NOT EXISTS sys_messages (
    id              BIGSERIAL       PRIMARY KEY,
    title           VARCHAR(500)    NOT NULL,
    content         TEXT            NOT NULL DEFAULT '',
    sender_id       BIGINT          NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    sender_name     VARCHAR(100)    NOT NULL,
    receiver_id     BIGINT          NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    receiver_name   VARCHAR(100)    NOT NULL,
    is_read         BOOLEAN         NOT NULL DEFAULT FALSE,
    read_at         TIMESTAMPTZ,
    parent_id       BIGINT          REFERENCES sys_messages(id),
    type            VARCHAR(20)     NOT NULL DEFAULT 'personal',
    priority        VARCHAR(20)     DEFAULT 'normal',
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      BIGINT          NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      BIGINT          NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_sys_messages_sender_id ON sys_messages(sender_id);
CREATE INDEX IF NOT EXISTS idx_sys_messages_receiver_id ON sys_messages(receiver_id);
CREATE INDEX IF NOT EXISTS idx_sys_messages_receiver_unread ON sys_messages(receiver_id, is_read) WHERE is_deleted = FALSE;
CREATE INDEX IF NOT EXISTS idx_sys_messages_parent_id ON sys_messages(parent_id);
CREATE INDEX IF NOT EXISTS idx_sys_messages_created_at ON sys_messages(created_at DESC);

COMMENT ON TABLE sys_messages IS '消息通知表';
COMMENT ON COLUMN sys_messages.title IS '消息标题';
COMMENT ON COLUMN sys_messages.content IS '消息内容';
COMMENT ON COLUMN sys_messages.sender_id IS '发送者用户ID';
COMMENT ON COLUMN sys_messages.sender_name IS '发送者用户名';
COMMENT ON COLUMN sys_messages.receiver_id IS '接收者用户ID';
COMMENT ON COLUMN sys_messages.receiver_name IS '接收者用户名';
COMMENT ON COLUMN sys_messages.is_read IS '是否已读';
COMMENT ON COLUMN sys_messages.read_at IS '阅读时间';
COMMENT ON COLUMN sys_messages.parent_id IS '回复的父消息ID';
COMMENT ON COLUMN sys_messages.type IS '消息类型: personal(个人)/system(系统)';
COMMENT ON COLUMN sys_messages.priority IS '优先级: low/normal/high/urgent';