CREATE TABLE identity_certs (
	id varchar(36) not null,
	created_at datetime(6),
	pair_type tinyint check (pair_type between 0 and 1) not null,
	private_key text not null,
	public_key text not null,
	primary key (id)
) engine=InnoDB;
