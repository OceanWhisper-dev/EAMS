INSERT INTO rpt_datasource (name, display_name, db_type, connection_string, description, sort_order, created_by)
VALUES
    ('main', 'main DB (PostgreSQL)', 'postgresql', 'Host=localhost;Port=5432;Database=eams2026;Username=postgres;Password=postgres', 'EAMS2026 main database', 1, 1),
    ('erp', 'ERP (SQL Server)', 'sqlserver', 'Server=db;Database=AppData;User ID=sa;Password=dfl_DB;TrustServerCertificate=True', 'EAMS 4.6 ERP database', 2, 1)
ON CONFLICT (name) WHERE is_deleted = FALSE DO NOTHING;