CREATE TABLE login_events (
    id bigint auto_increment primary key,
    login varchar(200) not null,
    active boolean not null default FALSE,
    ip varchar(50) not null,
    browser varchar(200) not null,
    platform varchar(200) not null,
    date timestamp not null,
    tenant_id bigint not null,
    user_id varchar(36) not null,
    page varchar(255),
    action integer not null default 0,
    description varchar(500)
);

CREATE INDEX idx_login_events_user_id ON login_events(user_id);
CREATE INDEX idx_login_events_tenant_id ON login_events(tenant_id);