ALTER TABLE identity_clients
    ADD COLUMN creator_id VARCHAR(36),
    ADD COLUMN modifier_id VARCHAR(36);

-- Note: DocSpace table that is created and removed here for migration purposes
CREATE TABLE IF NOT EXISTS core_user (
  id varchar(38) NOT NULL,
  tenant int NOT NULL,
  username varchar(255) NOT NULL,
  firstname varchar(64) NOT NULL,
  lastname varchar(64) NOT NULL,
  sex tinyint(1) DEFAULT NULL,
  bithdate datetime DEFAULT NULL,
  status int NOT NULL DEFAULT '1',
  activation_status int NOT NULL DEFAULT '0',
  email varchar(255) DEFAULT NULL,
  workfromdate datetime DEFAULT NULL,
  terminateddate datetime DEFAULT NULL,
  title varchar(64) DEFAULT NULL,
  culture varchar(20) DEFAULT NULL,
  contacts varchar(1024) DEFAULT NULL,
  phone varchar(255) DEFAULT NULL,
  phone_activation int NOT NULL DEFAULT '0',
  location varchar(255) DEFAULT NULL,
  notes varchar(512) DEFAULT NULL,
  sid varchar(512) DEFAULT NULL,
  sso_name_id varchar(512) DEFAULT NULL,
  sso_session_id varchar(512) DEFAULT NULL,
  removed tinyint(1) NOT NULL DEFAULT '0',
  create_on timestamp NOT NULL,
  last_modified datetime NOT NULL,
  PRIMARY KEY (id),
  KEY email (email),
  KEY last_modified (last_modified),
  KEY username (tenant,username),
  KEY tenant_activation_status_email (tenant, activation_status, email),
  KEY tenant_activation_status_firstname (tenant, activation_status, firstname),
  KEY tenant_activation_status_lastname (tenant, activation_status, lastname)
) ENGINE=InnoDB;

UPDATE identity_clients c
	JOIN core_user u ON c.created_by = u.email
	SET c.creator_id = u.id;

DROP TABLE core_user;

ALTER TABLE identity_clients
    DROP COLUMN created_by,
    DROP COLUMN modified_by;

ALTER TABLE identity_clients
    RENAME COLUMN creator_id TO created_by,
    RENAME COLUMN modifier_id TO modified_by;