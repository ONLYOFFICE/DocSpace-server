ALTER TABLE identity_authorizations
  ADD COLUMN id_token_value TEXT,
  ADD COLUMN id_token_claims TEXT,
  ADD COLUMN id_token_metadata TEXT,
  ADD COLUMN id_token_issued_at DATETIME(6),
  ADD COLUMN id_token_expires_at DATETIME(6);