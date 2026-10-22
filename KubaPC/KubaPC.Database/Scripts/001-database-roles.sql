DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'kubapc_app') THEN
        CREATE ROLE kubapc_app LOGIN NOINHERIT;
    ELSE
        ALTER ROLE kubapc_app NOINHERIT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'kubapc_administrator') THEN
        CREATE ROLE kubapc_administrator NOLOGIN;
    END IF;
END
$$;

GRANT kubapc_administrator TO kubapc_app;
