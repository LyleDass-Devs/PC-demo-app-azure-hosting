-- ============================================================================
--  013 - Rename operators.operator_code to id_societe
--
--  Aligns the as-built column with the functional specification and the
--  client's earlier databases, which both call the operator's business
--  identifier id_societe (societe is French for company, a term inherited
--  from the SAEIV system).
--
--  Scope deliberately narrow:
--    - operators.operator_code  -> id_societe (this script)
--    - operator_id FK columns   UNCHANGED (uuid pointers to operators.id,
--      an internal key the specification never names)
--    - saeiv_trip_exports.operator_code UNCHANGED (verbatim landing copy of
--      the source file's own column; evidence, renamed only if the source
--      file changes)
--
--  Run after 012. Safe to re-run.
-- ============================================================================

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'operators' AND column_name = 'operator_code')
    THEN
        ALTER TABLE operators RENAME COLUMN operator_code TO id_societe;
    END IF;
END $$;

ALTER INDEX IF EXISTS ux_operators_operator_code RENAME TO ux_operators_id_societe;

COMMENT ON COLUMN operators.id_societe IS
    'Operator business identifier as named by the functional specification and the SAEIV system (societe: French for company). Unique among active operators. Not the primary key: tables reference operators via the internal uuid operator_id.';

COMMENT ON TABLE operators IS
    'A.19. The transport operator. Its business identifier id_societe follows the functional specification and the SAEIV system; societe is French for company.';
