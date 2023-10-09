CREATE TABLE identity_authorizations (
	id varchar(255) not null,
	access_token_expires_at datetime(6),
	access_token_issued_at datetime(6),
	access_token_metadata text,
	access_token_scopes varchar(1000),
	access_token_type varchar(255),
	access_token_value text,
	attributes text,
	authorization_code_expires_at datetime(6),
	authorization_code_issued_at datetime(6),
	authorization_code_metadata varchar(255),
	authorization_code_value text,
	authorization_grant_type varchar(255),
	authorized_scopes varchar(1000),
	invalidated bit,
	modified_at datetime(6),
	principal_name varchar(255),
	refresh_token_expires_at datetime(6),
	refresh_token_issued_at datetime(6),
	refresh_token_metadata text,
	refresh_token_value text,
	registered_client_id varchar(255),
	state varchar(500),
	primary key (id)
) engine=InnoDB;

CREATE TABLE identity_clients (
	authentication_method varchar(100),
	client_id varchar(36) not null,
	client_issued_at datetime(6),
	client_secret varchar(36) not null,
	description varchar(255),
	logo_url varchar(255),
	logout_redirect_uri tinytext,
	client_name varchar(255),
	policy_url varchar(255),
	redirect_uri tinytext not null,
	scopes tinytext not null,
	terms_url varchar(255),
	tenant_id integer not null,
	invalidated bit,
	primary key (client_id)
) engine=InnoDB;

CREATE TABLE identity_consents (
	principal_name varchar(255) not null,
	registered_client_id varchar(255) not null,
	invalidated bit,
	modified_at datetime(6),
	scopes tinytext,
	primary key (principal_name, registered_client_id)
) engine=InnoDB;

ALTER TABLE identity_clients
	ADD CONSTRAINT UK_client_id
	UNIQUE (client_id);

ALTER TABLE identity_clients
	ADD CONSTRAINT UK_client_secret
	UNIQUE (client_secret);

ALTER TABLE identity_clients
	ADD CONSTRAINT FK_tenant_id__id
	FOREIGN KEY (tenant_id)
	REFERENCES tenants_tenants (id);

DROP EVENT IF EXISTS delete_invalidated_consents;
DROP EVENT IF EXISTS delete_invalidated_authorization;
DROP EVENT IF EXISTS delete_invalidated_clients;

CREATE EVENT delete_invalidated_consents
ON schedule at current_timestamp + interval 1 hour
    DO
		DELETE FROM identity_consents ic WHERE ic.invalidated = 1;

CREATE EVENT delete_invalidated_authorization
ON schedule at current_timestamp + interval 1 hour
    DO
      DELETE FROM identity_authorizations ia WHERE ia.invalidated = 1;

CREATE EVENT delete_invalidated_clients
ON schedule at current_timestamp + interval 1 hour
    DO
      DELETE FROM identity_clients ic WHERE ic.invalidated = 1;

DROP TRIGGER IF EXISTS update_entry_authorizations;
DROP TRIGGER IF EXISTS update_entry_consents;

DELIMITER $$
CREATE TRIGGER update_entry_authorizations
BEFORE UPDATE ON identity_authorizations
FOR EACH ROW
BEGIN
  IF new.modified_at <= old.modified_at
  THEN
   SIGNAL SQLSTATE '02000' SET MESSAGE_TEXT = 'Warning: updated date can not be before than existing date!';
  END IF;

END$$
DELIMITER ;

DELIMITER $$
CREATE TRIGGER update_entry_consents
BEFORE UPDATE ON identity_consents
FOR EACH ROW
BEGIN
  IF new.modified_at <= old.modified_at
  THEN
   SIGNAL SQLSTATE '02000' SET MESSAGE_TEXT = 'Warning: updated date can not be before than existing date!';
  END IF;

END$$
DELIMITER ;