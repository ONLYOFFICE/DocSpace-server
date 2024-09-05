-- Insert initial scope values into identity_scopes
INSERT INTO identity_scopes (name, `group`, `type`)
VALUES
    ('accounts:read', 'accounts', 'read'),
    ('accounts:write', 'accounts', 'write'),
    ('rooms:read', 'rooms', 'read'),
    ('rooms:write', 'rooms', 'write'),
    ('accounts.self:read', 'profiles', 'read'),
    ('accounts.self:write', 'profiles', 'write'),
    ('files:read', 'files', 'read'),
    ('files:write', 'files', 'write'),
    ('openid', 'openid', 'openid');