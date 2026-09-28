SET @ddl = IF(
    EXISTS (SELECT 1 FROM information_schema.tables
            WHERE table_schema = DATABASE() AND table_name = 'identity_client_authentication_methods')
    AND NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
            WHERE table_schema = DATABASE() AND table_name = 'identity_client_authentication_methods' AND constraint_type = 'PRIMARY KEY'),
    'ALTER TABLE identity_client_authentication_methods ADD COLUMN migration_row_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY',
    'DO 0');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @ddl = IF(
    EXISTS (SELECT 1 FROM information_schema.tables
            WHERE table_schema = DATABASE() AND table_name = 'identity_client_redirect_uris')
    AND NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
            WHERE table_schema = DATABASE() AND table_name = 'identity_client_redirect_uris' AND constraint_type = 'PRIMARY KEY'),
    'ALTER TABLE identity_client_redirect_uris ADD COLUMN migration_row_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY',
    'DO 0');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @ddl = IF(
    EXISTS (SELECT 1 FROM information_schema.tables
            WHERE table_schema = DATABASE() AND table_name = 'identity_client_allowed_origins')
    AND NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
            WHERE table_schema = DATABASE() AND table_name = 'identity_client_allowed_origins' AND constraint_type = 'PRIMARY KEY'),
    'ALTER TABLE identity_client_allowed_origins ADD COLUMN migration_row_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY',
    'DO 0');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @ddl = IF(
    EXISTS (SELECT 1 FROM information_schema.tables
            WHERE table_schema = DATABASE() AND table_name = 'identity_client_scopes')
    AND NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
            WHERE table_schema = DATABASE() AND table_name = 'identity_client_scopes' AND constraint_type = 'PRIMARY KEY'),
    'ALTER TABLE identity_client_scopes ADD COLUMN migration_row_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY',
    'DO 0');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
