INSERT INTO identity_scopes (name, `group`, `type`)
VALUES
    ('accounts:read', 'contacts', 'read'),
    ('accounts:write', 'contacts', 'write'),
    ('accounts.self:read', 'profiles', 'read'),
    ('accounts.self:write', 'profiles', 'write');

UPDATE identity_client_scopes
SET scope_name = REPLACE(scope_name, 'contacts', 'accounts')
WHERE scope_name IN (
    'contacts:read',
    'contacts:write',
    'contacts.self:read',
    'contacts.self:write'
);

UPDATE identity_consent_scopes
SET scopes = REPLACE(scopes, 'contacts', 'accounts')
WHERE scopes IN (
    'contacts:read',
    'contacts:write',
    'contacts.self:read',
    'contacts.self:write'
);

DELETE FROM identity_scopes
WHERE name IN (
    'contacts:read',
    'contacts:write',
    'contacts.self:read',
    'contacts.self:write'
);
