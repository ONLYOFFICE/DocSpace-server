#!/bin/bash
# The built-in forms database as a server shared by portals of different owners needs it: DocSpace connects as the
# owner of the database, not as a superuser, and the read-only roles of the portals cannot load the server.
# Runs once, on an empty data directory, after the image created $POSTGRES_DB.
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
    -v db="$POSTGRES_DB" \
    -v admin_user="$FORMS_ADMIN_USER" \
    -v admin_password="$FORMS_ADMIN_PASSWORD" \
    -v ro_timeout="$FORMS_RO_TIMEOUT" \
    -v ro_check="$FORMS_RO_CHECK" <<'EOSQL'
-- CREATEDB: a portal given direct access gets a database of its own, so no other portal shows in its catalog.
CREATE ROLE :"admin_user" LOGIN CREATEROLE CREATEDB PASSWORD :'admin_password';
ALTER DATABASE :"db" OWNER TO :"admin_user";

-- Step 3: the read-only roles connect to the forms database only.
REVOKE CONNECT ON DATABASE postgres, template1 FROM PUBLIC;

-- Step 2: no large objects.
REVOKE EXECUTE ON FUNCTION
    lo_create(oid),
    lo_creat(integer),
    lo_from_bytea(oid, bytea),
    lo_put(oid, bigint, bytea),
    lowrite(integer, bytea)
FROM PUBLIC;

-- Step 4: a read-only role lifts its own statement_timeout, so its long queries are ended from outside.
CREATE EXTENSION IF NOT EXISTS pg_cron;
SELECT cron.schedule('forms-ro-timeout', :'ro_check' || ' seconds',
    format($job$SELECT pg_terminate_backend(pid)
        FROM pg_stat_activity
        WHERE usename LIKE 'forms\_t%%\_ro'
          AND state <> 'idle'
          AND now() - query_start > interval %L$job$, :'ro_timeout' || ' seconds'));
EOSQL
