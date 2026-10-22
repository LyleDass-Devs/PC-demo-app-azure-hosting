DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'operators' AND column_name = 'operator_code')
    THEN
        ALTER TABLE operators RENAME COLUMN operator_code TO id_societe;
    END IF;
END $$;

ALTER INDEX IF EXISTS ux_operators_operator_code RENAME TO ux_operators_id_societe;
