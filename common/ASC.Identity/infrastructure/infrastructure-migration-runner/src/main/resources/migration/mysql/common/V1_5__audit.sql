CREATE TABLE audit_events (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    initiator VARCHAR(255),
    target VARCHAR(255),
    ip VARCHAR(255) NOT NULL,
    browser VARCHAR(255) NOT NULL,
    platform VARCHAR(255) NOT NULL,
    date TIMESTAMP NOT NULL,
    tenant_id BIGINT NOT NULL,
    user_id VARCHAR(255) NOT NULL,
    page VARCHAR(255) NOT NULL,
    action INT NOT NULL,
    description TEXT
);