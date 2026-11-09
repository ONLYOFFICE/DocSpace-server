CREATE TABLE identity_pending_messages (
    id BIGINT not null auto_increment,
    exchange_name varchar(255) not null,
    routing_key varchar(255) not null,
    headers TEXT not null,
    body MEDIUMBLOB not null,
    attempts INT not null default 0,
    deliver_at datetime(6) not null,
    created_at datetime(6) not null,
    primary key (id),
    index idx_identity_pending_messages_deliver_at (deliver_at)
) engine=InnoDB;
