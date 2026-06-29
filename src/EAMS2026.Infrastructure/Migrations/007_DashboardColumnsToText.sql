-- Change dashboard jsonb columns to text to avoid CAST issues with user input
ALTER TABLE dashboard_widgets ALTER COLUMN default_config TYPE TEXT;
ALTER TABLE dashboard_widgets ALTER COLUMN data_source_config TYPE TEXT;
ALTER TABLE dashboard_widgets ALTER COLUMN layout_config TYPE TEXT;
ALTER TABLE role_dashboard_config ALTER COLUMN config TYPE TEXT;