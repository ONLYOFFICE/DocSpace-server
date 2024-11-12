-- Insert initial scope values into identity_scopes
INSERT INTO identity_scopes (name, `group`, `type`)
VALUES
    ('contacts:read', 'contacts', 'read'),
    ('contacts:write', 'contacts', 'write'),
    ('rooms:read', 'rooms', 'read'),
    ('rooms:write', 'rooms', 'write'),
    ('contacts.self:read', 'profiles', 'read'),
    ('contacts.self:write', 'profiles', 'write'),
    ('files:read', 'files', 'read'),
    ('files:write', 'files', 'write'),
    ('openid', 'openid', 'openid');