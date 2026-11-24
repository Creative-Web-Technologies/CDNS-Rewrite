-- =============================================================================
-- cusdec-child-seed.sql
-- Dev-only seed for CusDec child tables: taxes, inspections, timelines, exit.
-- Targets: cus_dec_id IN (1,3,4,5,6,7,8) – external numbers read from cus_decs.
--
-- Reserved external_system_number bands (safe to cleanup by range):
--   cus_dec_inspect_statuses : 9 000 000 000 000 000 000 – 9 049 999 999 999 999 999
--     formula: 9000000000000000000 + cus_dec_id * 1 000 000 000 + step
--   cus_dec_statuses          : 9 050 000 000 000 000 000 – 9 099 999 999 999 999 999
--     formula: 9050000000000000000 + cus_dec_id * 1 000 000 000 + step
--
-- Text markers (SEED- prefix): tax_code, frd_nfr, section_name.
-- Section 4 (cus_dec_inspection_mappings / _section_tab_status_mappings) removed –
--   neither table is queried by any service (confirmed: AsycudaStatusResolver,
--   CusDecService, DashboardSupport all ignore them).
--
-- AsycudaStatusResolver matches on status code only (not description).
-- IsExited (CusDecService) checks s.Status directly against "Totally exited" /
--   "Cancelled" – so the exit row's status column must contain 'Totally exited'.
--
-- Expected row counts:
--   cus_dec_taxes                     :  32  (2×6 for CusDecs 1,4; 5×4 for 3,5,6,7,8)
--   cus_dec_inspections               :   2  (CusDecs 3, 5)
--   cus_dec_inspection_section_tab    :   4  (2 sections per inspection)
--   cus_dec_inspect_statuses          :   8  (4 steps × 2 CusDecs; reserved band)
--   cus_dec_statuses (CusDec 3)       :   2  (reserved band; steps 1–2)
--   cus_dec_statuses (CusDec 7)       :  40  (reserved band; steps 1–40)
--   cus_dec_statuses (CusDec 5 exit)  :   1  (reserved band; Totally exited)
--
-- Apply after: cusdec-ui-seed.sql
-- Remove with: cusdec-child-cleanup.sql
-- =============================================================================

BEGIN;

-- =============================================================================
-- SECTION 1: cus_dec_taxes
-- 2–3 tax lines per item for CusDecs 1 and 3–8 (7 CusDecs × 2 items each = 32 rows).
-- CusDecs 1 and 4: ranks 1–3 (3 taxes); all others: ranks 1–2 (2 taxes).
-- Guard: exact (cus_dec_id, item_number, tax_code, tax_rank) match prevents duplicates.
-- =============================================================================

INSERT INTO cus_dec_taxes (
  cus_dec_id, tax_code, tax_amount, last_sync_date,
  cus_dec_external_system_number, item_number, tax_rank
)
SELECT
  cd.cus_dec_id,
  'SEED-' || taxes.tax_abbr,
  taxes.tax_amt,
  NOW(),
  cd.cus_dec_external_system_number,
  cdi.cus_dec_item_number,
  taxes.tax_rank
FROM
  cus_decs cd
  JOIN cus_dec_items cdi ON cdi.cus_dec_id = cd.cus_dec_id
  CROSS JOIN (
    VALUES
      ('CID', 2500.00::numeric(18,2), 1),
      ('VAT', 1800.00::numeric(18,2), 2),
      ('PAL', 950.00::numeric(18,2), 3)
  ) AS taxes(tax_abbr, tax_amt, tax_rank)
WHERE
  cd.cus_dec_id IN (1, 3, 4, 5, 6, 7, 8)
  AND taxes.tax_rank <= CASE
    WHEN cd.cus_dec_id IN (1, 4) THEN 3
    ELSE 2
  END
  AND NOT EXISTS (
    SELECT 1 FROM cus_dec_taxes t2
    WHERE t2.cus_dec_id = cd.cus_dec_id
      AND t2.cus_dec_external_system_number = cd.cus_dec_external_system_number
      AND t2.item_number = cdi.cus_dec_item_number
      AND t2.tax_code = 'SEED-' || taxes.tax_abbr
      AND t2.tax_rank = taxes.tax_rank
  );

-- =============================================================================
-- SECTION 2: cus_dec_inspections + cus_dec_inspection_section_tab
-- 1 inspection with 2 sections on each of CusDec 3 and CusDec 5.
-- inspection_instance_id / ins_tab_external_system_number: cus_dec_id * 1000 + 1
--   CusDec 3 → 3001, CusDec 5 → 5001
-- Expected: 2 inspection rows + 4 section rows
-- Markers: frd_nfr / section_name start with 'SEED-'
-- =============================================================================

INSERT INTO cus_dec_inspections (
  issue_code, description, value, cus_dec_id, cus_dec_external_system_number,
  last_sync_date_time, inspection_instance_id, key_frd_number, frd_nfr, frd_uni, frd_ubr
)
SELECT
  'INS-' || d.cus_dec_id || '-01',
  'SEED-Inspection for unit testing on CusDec ' || d.cus_dec_id,
  1500.00::numeric(18,2),
  d.cus_dec_id,
  d.cus_dec_external_system_number,
  NOW(),
  (d.cus_dec_id::bigint * 1000 + 1),
  1,
  'SEED-INS-' || d.cus_dec_id || '-01',
  'FRD_UNI_01',
  'FRD_UBR_01'
FROM cus_decs d
WHERE d.cus_dec_id IN (3, 5)
  AND NOT EXISTS (
    SELECT 1 FROM cus_dec_inspections
    WHERE cus_dec_id = d.cus_dec_id
      AND frd_nfr = 'SEED-INS-' || d.cus_dec_id || '-01'
  );

INSERT INTO cus_dec_inspection_section_tab (
  cus_dec_id, cus_dec_external_system_number, ins_tab_external_system_number,
  key_sec_number, section_name, section_date, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.cus_dec_external_system_number,
  (d.cus_dec_id::bigint * 1000 + 1),
  sec_num,
  'SEED-SECT-' || sec_name,
  COALESCE(d.cus_dec_date, NOW()) - interval '1 day' + interval '8 hours' * sec_num,
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES (1, 'Item List'), (2, 'Goods Inspection')) AS sections(sec_num, sec_name)
WHERE d.cus_dec_id IN (3, 5)
  AND NOT EXISTS (
    SELECT 1 FROM cus_dec_inspection_section_tab
    WHERE cus_dec_id = d.cus_dec_id
      AND ins_tab_external_system_number = (d.cus_dec_id::bigint * 1000 + 1)
      AND key_sec_number = sec_num
  );

-- =============================================================================
-- SECTION 3: cus_dec_inspect_statuses
-- 4 status rows per inspection CusDec (3 and 5), including hidden code 600.
-- Reserved external_system_number: 9000000000000000000 + cus_dec_id * 1000000000 + step
--   CusDec 3: 9000000003000000001 – 9000000003000000004
--   CusDec 5: 9000000005000000001 – 9000000005000000004
-- table_name: 'UN_ASYBRK_TRACK' (INSP_ACT_TAB not referenced anywhere in the codebase;
--   the sync service reads from awunadm.UN_ASYBRK_TRACK for all document status rows)
-- Expected: 8 rows
-- =============================================================================

INSERT INTO cus_dec_inspect_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  last_sync_date_time, external_system_number, cus_dec_status_number,
  cus_dec_external_system_number, table_name
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.status_code,
  'SEED-Inspect status ' || s.status_name,
  COALESCE(d.cus_dec_date, NOW()) + s.step * interval '12 hours',
  NOW(),
  9000000000000000000::bigint + d.cus_dec_id::bigint * 1000000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK'
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Under Review'),
  (3, '600', 'Internal Check'),
  (4, '400', 'Cleared')
) AS s(step, status_code, status_name)
WHERE d.cus_dec_id IN (3, 5)
  AND NOT EXISTS (
    SELECT 1 FROM cus_dec_inspect_statuses
    WHERE external_system_number = 9000000000000000000::bigint + d.cus_dec_id::bigint * 1000000000 + s.step
  );

-- =============================================================================
-- SECTION 4 REMOVED
-- cus_dec_inspection_mappings and cus_dec_inspection_section_tab_status_mappings
-- are not read by any service (CusDecService, AsycudaStatusResolver,
-- DashboardSupport). Seeding them has no effect on UI behaviour.
-- =============================================================================

-- =============================================================================
-- SECTION 5: Timeline test data – cus_dec_statuses for CusDec 3
-- Adds 2 status rows (500 visible, 600 hidden) all newer than any existing row.
-- base_dt = GREATEST(max existing status_date_time, COALESCE(cus_dec_date, NOW()))
--           + 1 hour  → guarantees all seeded rows are newer than existing history.
-- Reserved external_system_number: 9050000000000000000 + cus_dec_id * 1000000000 + step
--   CusDec 3: 9050000003000000001 – 9050000003000000002
-- Expected: 2 rows
-- =============================================================================

WITH cus_dec_3 AS (
  SELECT
    cd.cus_dec_id,
    cd.global_identity,
    cd.cus_dec_external_system_number,
    GREATEST(
      COALESCE(
        (SELECT MAX(sx.status_date_time) FROM cus_dec_statuses sx WHERE sx.cus_dec_id = cd.cus_dec_id),
        COALESCE(cd.cus_dec_date, NOW())
      ),
      COALESCE(cd.cus_dec_date, NOW())
    ) + interval '1 hour' AS base_dt
  FROM cus_decs cd
  WHERE cd.cus_dec_id = 3
),
timeline_statuses AS (
  SELECT
    cd.cus_dec_id,
    cd.global_identity,
    cd.cus_dec_external_system_number,
    s.status_code,
    s.status_name,
    cd.base_dt + (s.step - 1) * interval '1 hour'                            AS status_date_time,
    9050000000000000000::bigint + cd.cus_dec_id::bigint * 1000000000 + s.step AS ext_num,
    s.step
  FROM cus_dec_3 cd
  CROSS JOIN (VALUES
    (1, '500', 'SEED-Document Under Review'),
    (2, '600', 'SEED-Internal Processing')
  ) AS s(step, status_code, status_name)
)
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  cus_dec_id,
  global_identity,
  status_code,
  status_name,
  status_date_time,
  ext_num,
  step,
  cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM timeline_statuses ts
WHERE NOT EXISTS (
  SELECT 1 FROM cus_dec_statuses ds
  WHERE ds.external_system_number = ts.ext_num
);

-- =============================================================================
-- SECTION 6: Timeline test data – 40 extra statuses for CusDec 7 (outlier test)
-- base_dt = GREATEST(max existing status_date_time, COALESCE(cus_dec_date, NOW()))
--           + 1 hour  → all 40 seeded rows are newer than every existing row.
-- Steps 1–10: visible codes cycling 200/300/400.
-- Steps 11–40: hidden code 600 (newest 30 are hidden, verifies TopK truncation).
-- Current visible status will be the last of steps 1–10 (step 10 → '300' Assessed).
-- Reserved external_system_number: 9050000000000000000 + cus_dec_id * 1000000000 + step
--   CusDec 7: 9050000007000000001 – 9050000007000000040
-- Guard: reserved ext_num (not cus_dec_status_number) → exactly 40 rows added.
-- Expected: 40 rows
-- =============================================================================

WITH cus_dec_7 AS (
  SELECT
    cd.cus_dec_id,
    cd.global_identity,
    cd.cus_dec_external_system_number,
    GREATEST(
      COALESCE(
        (SELECT MAX(sx.status_date_time) FROM cus_dec_statuses sx WHERE sx.cus_dec_id = cd.cus_dec_id),
        COALESCE(cd.cus_dec_date, NOW())
      ),
      COALESCE(cd.cus_dec_date, NOW())
    ) + interval '1 hour' AS base_dt
  FROM cus_decs cd
  WHERE cd.cus_dec_id = 7
)
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  cd.cus_dec_id,
  cd.global_identity,
  CASE
    WHEN step <= 10 THEN
      CASE ((step - 1) % 3)
        WHEN 0 THEN '200'
        WHEN 1 THEN '300'
        ELSE        '400'
      END
    ELSE '600'
  END,
  CASE
    WHEN step <= 10 THEN
      CASE ((step - 1) % 3)
        WHEN 0 THEN 'SEED-Registered step ' || step
        WHEN 1 THEN 'SEED-Assessed step ' || step
        ELSE        'SEED-Released step ' || step
      END
    ELSE 'SEED-Hidden step ' || step
  END,
  cd.base_dt + (step - 1) * interval '1 hour',
  9050000000000000000::bigint + cd.cus_dec_id::bigint * 1000000000 + step,
  step,
  cd.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM cus_dec_7 cd
CROSS JOIN generate_series(1, 40) AS step
WHERE NOT EXISTS (
  SELECT 1 FROM cus_dec_statuses ds
  WHERE ds.external_system_number = 9050000000000000000::bigint + cd.cus_dec_id::bigint * 1000000000 + step
);

-- =============================================================================
-- SECTION 7: Exit test – CusDec 5 "Totally exited" status row
-- IsExited (CusDecService.LoadExitedIdsAsync) checks s.Status == "Totally exited"
-- against the raw status column – the status code column must contain the string
-- 'Totally exited', not the numeric '700'.  AsycudaStatusResolver matches on
-- status code only (no description check); 'Totally exited' has no mapping row
-- and falls back to itself, so the display label is correct.
-- table_name: 'EXIT_NOTE_DETAIL_GENERAL_SEG' (matches DocTableFor("700") in seed).
-- base_dt = GREATEST(max existing status_date_time, COALESCE(cus_dec_date, NOW()))
--           + 1 hour  → seeded row is newer than all existing history.
-- Reserved external_system_number: 9050000005000000001
-- Expected: 1 row (guard on reserved ext_num, not on status code)
-- =============================================================================

WITH exit_base AS (
  SELECT
    cd.cus_dec_id,
    cd.global_identity,
    cd.cus_dec_external_system_number,
    GREATEST(
      COALESCE(
        (SELECT MAX(sx.status_date_time) FROM cus_dec_statuses sx WHERE sx.cus_dec_id = cd.cus_dec_id),
        COALESCE(cd.cus_dec_date, NOW())
      ),
      COALESCE(cd.cus_dec_date, NOW())
    ) + interval '1 hour' AS base_dt
  FROM cus_decs cd
  WHERE cd.cus_dec_id = 5
)
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  eb.cus_dec_id,
  eb.global_identity,
  'Totally exited',
  'ASYCUDA status 700',
  eb.base_dt,
  9050000000000000000::bigint + eb.cus_dec_id::bigint * 1000000000 + 1,
  100,
  eb.cus_dec_external_system_number,
  'EXIT_NOTE_DETAIL_GENERAL_SEG',
  NOW()
FROM exit_base eb
WHERE NOT EXISTS (
  SELECT 1 FROM cus_dec_statuses ds
  WHERE ds.external_system_number = 9050000000000000000::bigint + eb.cus_dec_id::bigint * 1000000000 + 1
);

-- =============================================================================
-- Final summary: row counts per table (expected totals in header above)
-- =============================================================================

SELECT
  'cus_dec_taxes' AS table_name,
  COUNT(*) AS marked_rows
FROM cus_dec_taxes
WHERE tax_code LIKE 'SEED-%'

UNION ALL

SELECT 'cus_dec_inspections', COUNT(*)
FROM cus_dec_inspections
WHERE frd_nfr LIKE 'SEED-%'

UNION ALL

SELECT 'cus_dec_inspection_section_tab', COUNT(*)
FROM cus_dec_inspection_section_tab
WHERE section_name LIKE 'SEED-%'

UNION ALL

SELECT 'cus_dec_inspect_statuses', COUNT(*)
FROM cus_dec_inspect_statuses
WHERE cus_dec_id IN (3, 5)
  AND external_system_number BETWEEN 9000000000000000000 AND 9049999999999999999

UNION ALL

SELECT 'cus_dec_statuses', COUNT(*)
FROM cus_dec_statuses
WHERE cus_dec_id IN (3, 5, 7)
  AND external_system_number BETWEEN 9050000000000000000 AND 9099999999999999999

;

COMMIT;
