-- ============================================================
-- Migration 040: 修复所有序列值（确保 nextval 不产生重复主键）
-- 
-- 背景：表重命名后，序列的 last_value 可能小于 max(id)，
-- 导致 INSERT ... RETURNING id 时主键冲突。
-- 例：users_id_seq last_value=5, sys_users.max(id)=30 → 修复为 31
-- ============================================================

DO $$
DECLARE
    rec RECORD;
    seq_name TEXT;
    max_id BIGINT;
    last_val BIGINT;
BEGIN
    FOR rec IN 
        SELECT c.table_name, c.column_name, c.column_default
        FROM information_schema.columns c
        JOIN information_schema.tables t 
            ON c.table_name = t.table_name AND t.table_schema = 'public'
        WHERE c.column_default LIKE 'nextval(%'
          AND c.table_schema = 'public'
          AND t.table_type = 'BASE TABLE'
    LOOP
        seq_name := substring(rec.column_default FROM 'nextval\(''([^'']+)''');

        EXECUTE format('SELECT COALESCE(MAX(%I), 0) FROM %I', rec.column_name, rec.table_name) INTO max_id;

        BEGIN
            EXECUTE format('SELECT last_value FROM %I', seq_name) INTO last_val;
        EXCEPTION WHEN undefined_table THEN
            RAISE NOTICE 'SKIP:   % (seq % not found)', rec.table_name, seq_name;
            CONTINUE;
        END;

        IF max_id >= last_val THEN
            EXECUTE format('SELECT setval(%L, %s)', seq_name, max_id + 1);
            RAISE NOTICE 'FIXED:  %.% (seq=%) last=% → %', rec.table_name, rec.column_name, seq_name, last_val, max_id + 1;
        ELSE
            RAISE NOTICE 'OK:     %.% (seq=%) last=%', rec.table_name, rec.column_name, seq_name, last_val;
        END IF;
    END LOOP;
END $$;