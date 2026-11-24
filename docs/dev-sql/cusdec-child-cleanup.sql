-- =============================================================================
-- cusdec-child-cleanup.sql
-- Cleanup for cusdec-child-seed.sql: removes only marked/reserved seed rows.
-- FK-safe order: children first (statuses → inspection sections → inspections
--                                → taxes).
-- Safe to run on its own; safely idempotent if nothing has been seeded.
--
-- Deletion strategy per table:
--   cus_dec_inspect_statuses  – reserved ext_num band + cus_dec_id IN (3,5)
--   cus_dec_statuses          – reserved ext_num band + cus_dec_id IN (3,5,7)
--   cus_dec_inspection_section_tab – SEED- section_name marker
--   cus_dec_inspections            – SEED- frd_nfr marker
--   cus_dec_taxes                  – SEED- tax_code marker
--
-- Never deletes: pre-existing statuses, existing code-700 rows seeded by
-- MemberPortalSeedData, or any row outside the reserved ext_num bands.
-- =============================================================================

-- =============================================================================
-- Preview: counts of rows that will be deleted
-- =============================================================================

SELECT 'Preview of rows to delete:' AS info;

SELECT
  'cus_dec_inspect_statuses' AS table_name,
  COUNT(*) AS rows_to_delete
FROM cus_dec_inspect_statuses
WHERE cus_dec_id IN (3, 5)
  AND external_system_number BETWEEN 9000000000000000000 AND 9049999999999999999

UNION ALL

SELECT 'cus_dec_statuses', COUNT(*)
FROM cus_dec_statuses
WHERE cus_dec_id IN (3, 5, 7)
  AND external_system_number BETWEEN 9050000000000000000 AND 9099999999999999999

UNION ALL

SELECT 'cus_dec_inspection_section_tab', COUNT(*)
FROM cus_dec_inspection_section_tab
WHERE section_name LIKE 'SEED-%'

UNION ALL

SELECT 'cus_dec_inspections', COUNT(*)
FROM cus_dec_inspections
WHERE frd_nfr LIKE 'SEED-%'

UNION ALL

SELECT 'cus_dec_taxes', COUNT(*)
FROM cus_dec_taxes
WHERE tax_code LIKE 'SEED-%'

;

-- =============================================================================
-- Begin transaction and delete in FK-safe order
-- =============================================================================

BEGIN;

-- ── Inspection statuses (FK to cus_dec_inspections → cus_decs) ───────────────
DELETE FROM cus_dec_inspect_statuses
WHERE cus_dec_id IN (3, 5)
  AND external_system_number BETWEEN 9000000000000000000 AND 9049999999999999999;

-- ── Document statuses for timeline / exit CusDecs (FK to cus_decs) ───────────
DELETE FROM cus_dec_statuses
WHERE cus_dec_id IN (3, 5, 7)
  AND external_system_number BETWEEN 9050000000000000000 AND 9099999999999999999;

-- ── Inspection sections (FK to cus_dec_inspections) ───────────────────────────
DELETE FROM cus_dec_inspection_section_tab
WHERE section_name LIKE 'SEED-%';

-- ── Inspections (FK to cus_decs) ──────────────────────────────────────────────
DELETE FROM cus_dec_inspections
WHERE frd_nfr LIKE 'SEED-%';

-- ── Taxes (FK to cus_decs) ────────────────────────────────────────────────────
DELETE FROM cus_dec_taxes
WHERE tax_code LIKE 'SEED-%';

COMMIT;

-- =============================================================================
-- Verification: confirm all marked/reserved rows are deleted (should all be 0)
-- =============================================================================

SELECT 'Verification – should all be 0:' AS info;

SELECT
  'cus_dec_inspect_statuses' AS table_name,
  COUNT(*) AS remaining_rows
FROM cus_dec_inspect_statuses
WHERE cus_dec_id IN (3, 5)
  AND external_system_number BETWEEN 9000000000000000000 AND 9049999999999999999

UNION ALL

SELECT 'cus_dec_statuses', COUNT(*)
FROM cus_dec_statuses
WHERE cus_dec_id IN (3, 5, 7)
  AND external_system_number BETWEEN 9050000000000000000 AND 9099999999999999999

UNION ALL

SELECT 'cus_dec_inspection_section_tab', COUNT(*)
FROM cus_dec_inspection_section_tab
WHERE section_name LIKE 'SEED-%'

UNION ALL

SELECT 'cus_dec_inspections', COUNT(*)
FROM cus_dec_inspections
WHERE frd_nfr LIKE 'SEED-%'

UNION ALL

SELECT 'cus_dec_taxes', COUNT(*)
FROM cus_dec_taxes
WHERE tax_code LIKE 'SEED-%'

;
