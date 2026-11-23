-- =============================================================================
-- cusdec-ui-seed.sql
-- Dev-only seed for CusDec UI testing (tabs, independence, outlier, subscriptions).
-- External-system-number range used: 960000001 – 960000070 (inclusive).
-- Apply once on a fresh-seeded DB.  Remove with: cusdec-ui-seed-cleanup.sql
-- =============================================================================

BEGIN;

-- =============================================================================
-- GUARD: abort if any CusDec in our range already exists
-- =============================================================================
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM cus_decs
    WHERE cus_dec_external_system_number >= 960000001
  ) THEN
    RAISE EXCEPTION
      'cusdec-ui-seed already applied: '
      'rows with cus_dec_external_system_number >= 960000001 exist in cus_decs. '
      'Run docs/dev-sql/cusdec-ui-seed-cleanup.sql first.';
  END IF;
END;
$$;

-- =============================================================================
-- HELPER TEMP TABLE: resolve member / company IDs once
-- (all scalar sub-selects; this table always has exactly 1 row)
-- =============================================================================
CREATE TEMP TABLE _seed_ids AS
SELECT
  (SELECT member_id  FROM members   WHERE email       = 'existingmember-a@test.cdns')     AS member2_id,
  (SELECT company_id FROM companies WHERE tin_number  = '100000001')                       AS co1_id,
  (SELECT company_name FROM companies WHERE tin_number = '100000001')                      AS co1_name,
  (SELECT company_id FROM companies WHERE tin_number  = '100000002')                       AS co2_id,
  (SELECT company_name FROM companies WHERE tin_number = '100000002')                      AS co2_name,
  (SELECT cmm.member_id
     FROM company_member_mappings cmm
     JOIN companies c ON c.company_id = cmm.company_id
    WHERE c.tin_number = '100000001' AND NOT cmm.is_deleted
    LIMIT 1)                                                                                AS master_a_id,
  (SELECT cmm.member_id
     FROM company_member_mappings cmm
     JOIN companies c ON c.company_id = cmm.company_id
    WHERE c.tin_number = '100000002' AND NOT cmm.is_deleted
    LIMIT 1)                                                                                AS master_b_id;

-- =============================================================================
-- SECTION 1: Hidden status mapping  (code 600, is_show = false)
-- Expected: 1 new row in cus_dec_status_mappings
-- The 6 existing rows (200/300/400/500/700/900) are untouched.
-- =============================================================================
INSERT INTO cus_dec_status_mappings
  (asycuda_status, asycuda_description, cdns_status, cdns_description, is_show, office_code)
VALUES
  ('600', 'ASYCUDA status 600', 'Internal check', 'Internal check', false, NULL);


-- =============================================================================
-- SECTION 2: Completed CusDecs
-- (claimed + not disclaimed + completed_date_time set, on the relevant side)
--
-- External number layout:
--   960000001 – 960000020  Declarant co1,  WITH 700 history  (20 rows)
--   960000021 – 960000040  Declarant co1,  WITHOUT 700       (20 rows)
--   960000041 – 960000043  Consignee  co2, WITH 700 history  ( 3 rows)
--   960000044 – 960000046  Consignee  co2, WITHOUT 700       ( 3 rows)
--
-- After this section: Declarant co1 Completed = 40, Consignee co2 Completed = 6
-- =============================================================================

-- ── 2a: 20 Declarant co1 Completed  WITH 700  (ext 960000001–960000020) ──────
-- Expected: 20 cus_decs + 60 status rows (3 × 20)
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date, release_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  declarant_company_id, declarant_tin_number, declarant_company_name,
  consignee_tin_number, consignee_company_name,
  declarant_claimed_by_id,  declarant_claimed_date_time,
  declarant_completed_by_id, declarant_completed_date_time,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S' || lpad(n::text, 6, '0'),
  gen_random_uuid(),
  960000000 + n,
  NOW(),
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day' + interval '16 hours',
  'CBHQ1', 'CN', 'LK', 'Green', 'CIF', 'IM4',
  (20000 + n * 500)::numeric(18,2),
  'BL' || (960000000 + n),
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co1_id, '100000001', si.co1_name,
  '700000000', 'Unrelated Importer Pvt Ltd',
  si.member2_id, NOW() - n * interval '1 day' + interval '1 hour',
  si.member2_id, NOW() - n * interval '1 day' + interval '20 hours',
  0, 0
FROM generate_series(1, 20) AS n
CROSS JOIN _seed_ids si;

-- Status rows: 200 → 300 → 700
-- event_external_id = cus_dec_external_system_number * 10000000 + step  (kind=0, a=0)
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.code,
  s.label,
  d.cus_dec_date + s.step * interval '8 hours',
  d.cus_dec_external_system_number * 10000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  CASE WHEN s.code = '700' THEN 'EXIT_NOTE_DETAIL_GENERAL_SEG' ELSE 'UN_ASYBRK_TRACK' END,
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Assessed'),
  (3, '700', 'Totally exited')
) AS s(step, code, label)
WHERE d.cus_dec_external_system_number BETWEEN 960000001 AND 960000020;

-- ── 2b: 20 Declarant co1 Completed  WITHOUT 700  (ext 960000021–960000040) ───
-- Expected: 20 cus_decs + 60 status rows (3 × 20: 200, 300, 400)
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date, release_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  declarant_company_id, declarant_tin_number, declarant_company_name,
  consignee_tin_number, consignee_company_name,
  declarant_claimed_by_id,  declarant_claimed_date_time,
  declarant_completed_by_id, declarant_completed_date_time,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S' || lpad(n::text, 6, '0'),
  gen_random_uuid(),
  960000000 + n,
  NOW(),
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day' + interval '16 hours',
  'CBHQ1', 'CN', 'LK', 'Green', 'CIF', 'IM4',
  (20000 + n * 500)::numeric(18,2),
  'BL' || (960000000 + n),
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co1_id, '100000001', si.co1_name,
  '700000000', 'Unrelated Importer Pvt Ltd',
  si.member2_id, NOW() - n * interval '1 day' + interval '1 hour',
  si.member2_id, NOW() - n * interval '1 day' + interval '20 hours',
  0, 0
FROM generate_series(21, 40) AS n
CROSS JOIN _seed_ids si;

-- Status rows: 200 → 300 → 400  (released but not exited)
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.code,
  s.label,
  d.cus_dec_date + s.step * interval '8 hours',
  d.cus_dec_external_system_number * 10000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Assessed'),
  (3, '400', 'Released')
) AS s(step, code, label)
WHERE d.cus_dec_external_system_number BETWEEN 960000021 AND 960000040;

-- ── 2c: 3 Consignee co2 Completed  WITH 700  (ext 960000041–960000043) ───────
-- Expected: 3 cus_decs + 9 status rows
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date, release_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  consignee_company_id, consignee_tin_number, consignee_company_name,
  declarant_tin_number, declarant_company_name,
  consignee_claimed_by_id,  consignee_claimed_date_time,
  consignee_completed_by_id, consignee_completed_date_time,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S' || lpad(n::text, 6, '0'),
  gen_random_uuid(),
  960000000 + n,
  NOW(),
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day' + interval '16 hours',
  'CBHQ1', 'CN', 'LK', 'Yellow', 'CIF', 'IM4',
  (20000 + n * 500)::numeric(18,2),
  'BL' || (960000000 + n),
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co2_id, '100000002', si.co2_name,
  '800000000', 'Unrelated Clearing Agent',
  si.member2_id, NOW() - n * interval '1 day' + interval '1 hour',
  si.member2_id, NOW() - n * interval '1 day' + interval '20 hours',
  0, 0
FROM generate_series(41, 43) AS n
CROSS JOIN _seed_ids si;

-- Status rows: 200 → 300 → 700
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.code,
  s.label,
  d.cus_dec_date + s.step * interval '8 hours',
  d.cus_dec_external_system_number * 10000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  CASE WHEN s.code = '700' THEN 'EXIT_NOTE_DETAIL_GENERAL_SEG' ELSE 'UN_ASYBRK_TRACK' END,
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Assessed'),
  (3, '700', 'Totally exited')
) AS s(step, code, label)
WHERE d.cus_dec_external_system_number BETWEEN 960000041 AND 960000043;

-- ── 2d: 3 Consignee co2 Completed  WITHOUT 700  (ext 960000044–960000046) ────
-- Expected: 3 cus_decs + 9 status rows
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date, release_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  consignee_company_id, consignee_tin_number, consignee_company_name,
  declarant_tin_number, declarant_company_name,
  consignee_claimed_by_id,  consignee_claimed_date_time,
  consignee_completed_by_id, consignee_completed_date_time,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S' || lpad(n::text, 6, '0'),
  gen_random_uuid(),
  960000000 + n,
  NOW(),
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day' + interval '16 hours',
  'CBHQ1', 'CN', 'LK', 'Yellow', 'CIF', 'IM4',
  (20000 + n * 500)::numeric(18,2),
  'BL' || (960000000 + n),
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co2_id, '100000002', si.co2_name,
  '800000000', 'Unrelated Clearing Agent',
  si.member2_id, NOW() - n * interval '1 day' + interval '1 hour',
  si.member2_id, NOW() - n * interval '1 day' + interval '20 hours',
  0, 0
FROM generate_series(44, 46) AS n
CROSS JOIN _seed_ids si;

-- Status rows: 200 → 300 → 400
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.code,
  s.label,
  d.cus_dec_date + s.step * interval '8 hours',
  d.cus_dec_external_system_number * 10000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Assessed'),
  (3, '400', 'Released')
) AS s(step, code, label)
WHERE d.cus_dec_external_system_number BETWEEN 960000044 AND 960000046;


-- =============================================================================
-- SECTION 3: Additional Disclaimed CusDecs
-- (disclaimed_by_id IS NOT NULL on the relevant side)
--
-- External number layout:
--   960000047 – 960000051  Declarant co1  (+5 disclaimed)
--   960000052 – 960000053  Consignee  co2 (+2 disclaimed)
--
-- After this section: Declarant co1 Disclaimed = 1+5 = 6, Consignee co2 Disclaimed = 1+2 = 3
-- =============================================================================

-- ── 3a: 5 Declarant co1 Disclaimed  (ext 960000047–960000051) ────────────────
-- Expected: 5 cus_decs + 10 status rows (2 × 5)
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  declarant_company_id, declarant_tin_number, declarant_company_name,
  consignee_tin_number, consignee_company_name,
  declarant_disclaimed_by_id, declarant_disclaimed_date_time,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S' || lpad(n::text, 6, '0'),
  gen_random_uuid(),
  960000000 + n,
  NOW(),
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day',
  'CBHQ1', 'CN', 'LK', 'Green', 'CIF', 'IM4',
  (20000 + n * 500)::numeric(18,2),
  'BL' || (960000000 + n),
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co1_id, '100000001', si.co1_name,
  '700000000', 'Unrelated Importer Pvt Ltd',
  si.member2_id, NOW() - n * interval '1 day' + interval '1 hour',
  0, 0
FROM generate_series(47, 51) AS n
CROSS JOIN _seed_ids si;

-- Status rows: 200 → 300
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.code,
  s.label,
  d.cus_dec_date + s.step * interval '8 hours',
  d.cus_dec_external_system_number * 10000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Assessed')
) AS s(step, code, label)
WHERE d.cus_dec_external_system_number BETWEEN 960000047 AND 960000051;

-- ── 3b: 2 Consignee co2 Disclaimed  (ext 960000052–960000053) ────────────────
-- Expected: 2 cus_decs + 4 status rows
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  consignee_company_id, consignee_tin_number, consignee_company_name,
  declarant_tin_number, declarant_company_name,
  consignee_disclaimed_by_id, consignee_disclaimed_date_time,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S' || lpad(n::text, 6, '0'),
  gen_random_uuid(),
  960000000 + n,
  NOW(),
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day',
  'CBHQ1', 'CN', 'LK', 'Yellow', 'CIF', 'IM4',
  (20000 + n * 500)::numeric(18,2),
  'BL' || (960000000 + n),
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co2_id, '100000002', si.co2_name,
  '800000000', 'Unrelated Clearing Agent',
  si.member2_id, NOW() - n * interval '1 day' + interval '1 hour',
  0, 0
FROM generate_series(52, 53) AS n
CROSS JOIN _seed_ids si;

-- Status rows: 200 → 300
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.code,
  s.label,
  d.cus_dec_date + s.step * interval '8 hours',
  d.cus_dec_external_system_number * 10000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Assessed')
) AS s(step, code, label)
WHERE d.cus_dec_external_system_number BETWEEN 960000052 AND 960000053;


-- =============================================================================
-- SECTION 4: New CusDecs
-- (claimed_by_id IS NULL AND disclaimed_by_id IS NULL on the relevant side)
--
-- External number layout:
--   960000054 – 960000059  Consignee co2 New (+6)
--   960000060 – 960000063  Declarant co2 New (+4)
--
-- After this section: Consignee co2 New = 6, Declarant co2 New = 4
-- =============================================================================

-- ── 4a: 6 Consignee co2 New  (ext 960000054–960000059) ───────────────────────
-- Expected: 6 cus_decs + 12 status rows (2 × 6)
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  consignee_company_id, consignee_tin_number, consignee_company_name,
  declarant_tin_number, declarant_company_name,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S' || lpad(n::text, 6, '0'),
  gen_random_uuid(),
  960000000 + n,
  NOW(),
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day',
  'CBHQ1', 'CN', 'LK', 'Yellow', 'CIF', 'IM4',
  (20000 + n * 500)::numeric(18,2),
  'BL' || (960000000 + n),
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co2_id, '100000002', si.co2_name,
  '800000000', 'Unrelated Clearing Agent',
  0, 0
FROM generate_series(54, 59) AS n
CROSS JOIN _seed_ids si;

-- Status rows: 200 → 300
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.code,
  s.label,
  d.cus_dec_date + s.step * interval '8 hours',
  d.cus_dec_external_system_number * 10000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Assessed')
) AS s(step, code, label)
WHERE d.cus_dec_external_system_number BETWEEN 960000054 AND 960000059;

-- ── 4b: 4 Declarant co2 New  (ext 960000060–960000063) ───────────────────────
-- Expected: 4 cus_decs + 8 status rows (2 × 4)
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  declarant_company_id, declarant_tin_number, declarant_company_name,
  consignee_tin_number, consignee_company_name,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S' || lpad(n::text, 6, '0'),
  gen_random_uuid(),
  960000000 + n,
  NOW(),
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day',
  'CBHQ1', 'CN', 'LK', 'Green', 'CIF', 'IM4',
  (20000 + n * 500)::numeric(18,2),
  'BL' || (960000000 + n),
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co2_id, '100000002', si.co2_name,
  '700000000', 'Unrelated Importer Pvt Ltd',
  0, 0
FROM generate_series(60, 63) AS n
CROSS JOIN _seed_ids si;

-- Status rows: 200 → 300
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.code,
  s.label,
  d.cus_dec_date + s.step * interval '8 hours',
  d.cus_dec_external_system_number * 10000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Assessed')
) AS s(step, code, label)
WHERE d.cus_dec_external_system_number BETWEEN 960000060 AND 960000063;


-- =============================================================================
-- SECTION 5: Independence cases
-- Each CusDec has BOTH declarant_company_id (co1) AND consignee_company_id (co2).
-- The claim columns on each side are set independently.
--
-- External number layout:
--   960000064 – 960000066  Decl co1 In-Progress + Cons co2 New  (3 rows)
--   960000067 – 960000069  Cons co2 In-Progress + Decl co1 New  (3 rows)
--
-- After this section:
--   Declarant co1 In-Progress += 3   →  1 + 3 = 4
--   Consignee  co2 New         += 3   →  6 + 3 = 9
--   Consignee  co2 In-Progress += 3   →  6 + 3 = 9  (pre-existing 6 from MemberPortalSeedData)
--   Declarant  co1 New         += 3   →  9 + 3 = 12
-- =============================================================================

-- ── 5a: Declarant co1 In-Progress  +  Consignee co2 New  (ext 960000064–960000066)
-- Expected: 3 cus_decs + 9 status rows (3 × 3)
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  declarant_company_id, declarant_tin_number, declarant_company_name,
  consignee_company_id, consignee_tin_number, consignee_company_name,
  declarant_claimed_by_id, declarant_claimed_date_time,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S' || lpad(n::text, 6, '0'),
  gen_random_uuid(),
  960000000 + n,
  NOW(),
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day',
  'CBHQ1', 'CN', 'LK', 'Green', 'CIF', 'IM4',
  (20000 + n * 500)::numeric(18,2),
  'BL' || (960000000 + n),
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co1_id, '100000001', si.co1_name,
  si.co2_id, '100000002', si.co2_name,
  si.member2_id, NOW() - n * interval '1 day' + interval '2 hours',
  0, 0
FROM generate_series(64, 66) AS n
CROSS JOIN _seed_ids si;

-- Status rows: 200 → 300 → 400
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.code,
  s.label,
  d.cus_dec_date + s.step * interval '8 hours',
  d.cus_dec_external_system_number * 10000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Assessed'),
  (3, '400', 'Released')
) AS s(step, code, label)
WHERE d.cus_dec_external_system_number BETWEEN 960000064 AND 960000066;

-- ── 5b: Consignee co2 In-Progress  +  Declarant co1 New  (ext 960000067–960000069)
-- Expected: 3 cus_decs + 9 status rows
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  declarant_company_id, declarant_tin_number, declarant_company_name,
  consignee_company_id, consignee_tin_number, consignee_company_name,
  consignee_claimed_by_id, consignee_claimed_date_time,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S' || lpad(n::text, 6, '0'),
  gen_random_uuid(),
  960000000 + n,
  NOW(),
  NOW() - n * interval '1 day',
  NOW() - n * interval '1 day',
  'CBHQ1', 'CN', 'LK', 'Yellow', 'CIF', 'IM4',
  (20000 + n * 500)::numeric(18,2),
  'BL' || (960000000 + n),
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co1_id, '100000001', si.co1_name,
  si.co2_id, '100000002', si.co2_name,
  si.member2_id, NOW() - n * interval '1 day' + interval '2 hours',
  0, 0
FROM generate_series(67, 69) AS n
CROSS JOIN _seed_ids si;

-- Status rows: 200 → 300 → 400
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
  s.code,
  s.label,
  d.cus_dec_date + s.step * interval '8 hours',
  d.cus_dec_external_system_number * 10000000 + s.step,
  s.step,
  d.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM cus_decs d
CROSS JOIN (VALUES
  (1, '200', 'Registered'),
  (2, '300', 'Assessed'),
  (3, '400', 'Released')
) AS s(step, code, label)
WHERE d.cus_dec_external_system_number BETWEEN 960000067 AND 960000069;


-- =============================================================================
-- SECTION 6: Outlier
-- 1 Declarant co1 In-Progress CusDec with 45 status rows.
--   Steps  1–10: visible (200/300/400 cycling, increasing timestamps)
--   Steps 11–45: hidden code 600 (is_show = false), newer timestamps
-- Current status resolves to step 10 (latest visible = 'Registered', code 200).
-- AsycudaStatusResolver skips hidden rows ordered by status_date_time DESC.
-- Expected: 1 cus_dec + 45 status rows
-- After this section: Declarant co1 In-Progress = 4 + 1 = 5
-- =============================================================================
INSERT INTO cus_decs (
  cus_dec_number, global_identity, cus_dec_external_system_number, last_sync_date_time,
  cus_dec_date, registration_date,
  office_code, origin, destination, channel, incoterm, cus_dec_type,
  invoice_value, bl_number, exporter, supplier_company_name,
  declarant_company_id, declarant_tin_number, declarant_company_name,
  consignee_tin_number, consignee_company_name,
  declarant_claimed_by_id, declarant_claimed_date_time,
  no_of_items, no_of_containers
)
SELECT
  'CUSDEC-2026-S000070',
  gen_random_uuid(),
  960000070,
  NOW(),
  NOW() - interval '70 days',
  NOW() - interval '70 days',
  'CBHQ1', 'CN', 'LK', 'Green', 'CIF', 'IM4',
  55000::numeric(18,2),
  'BL960000070',
  'Overseas Supplier Co.', 'Overseas Supplier Co.',
  si.co1_id, '100000001', si.co1_name,
  '700000000', 'Unrelated Importer Pvt Ltd',
  si.member2_id, NOW() - interval '70 days' + interval '2 hours',
  0, 0
FROM _seed_ids si;

-- Status rows (45 total):
--   Steps 1–10:  visible — codes 200/300/400 cycling  (table = UN_ASYBRK_TRACK)
--   Steps 11–45: hidden  — code 600                   (table = UN_ASYBRK_TRACK)
-- step 10 → (10-1)%3 = 0 → code '200' → CDNS label 'Registered' = current visible status
INSERT INTO cus_dec_statuses (
  cus_dec_id, cus_dec_global_identity, status, description, status_date_time,
  external_system_number, cus_dec_status_number, cus_dec_external_system_number,
  table_name, last_sync_date_time
)
SELECT
  d.cus_dec_id,
  d.global_identity,
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
        WHEN 0 THEN 'Registered'
        WHEN 1 THEN 'Assessed'
        ELSE        'Released'
      END
    ELSE 'Internal check'
  END,
  d.cus_dec_date + step * interval '1 hour',
  d.cus_dec_external_system_number * 10000000 + step,
  step,
  d.cus_dec_external_system_number,
  'UN_ASYBRK_TRACK',
  NOW()
FROM cus_decs d
CROSS JOIN generate_series(1, 45) AS step
WHERE d.cus_dec_external_system_number = 960000070;


-- =============================================================================
-- SECTION 7: Subscriptions
-- Uses a DO block to capture auto-generated identity column values.
--
-- Rows added:
--   subscriptions        +1  "Declarant Annual (Acknowledgement Only)"
--                            subscription_action_type_enum = 1 (OnlyForAcknowledgement)
--                            subscription_type_enum = 1 (Declarant)
--                            Used for member 2 + company 1 subscription below.
--
--   subscription_requests +3  (one per new member_subscription)
--   member_subscriptions  +3:
--     (a) member 2,    company 1, Declarant, OnlyForAcknowledgement=1 (approved by master-a)
--     (b) master-a,   company 1, Declarant, General=0 — self-subscription using
--                     "Declarant Annual" (subscription_action_type_enum=0, type=1)
--     (c) master-b,   company 2, Consignee, General=0 — self-subscription using
--                     "Consignee Annual"  (subscription_action_type_enum=0, type=2)
--
-- key column is intentionally NULL on all three rows.
-- Call POST /api/dev/subscriptions/rekey after applying to generate HMAC keys.
-- =============================================================================
DO $$
DECLARE
  v_member2_id    bigint;
  v_co1_id        int;
  v_co2_id        int;
  v_master_a_id   bigint;
  v_master_b_id   bigint;
  v_ack_plan_id   int;
  v_decl_plan_id  int;
  v_cons_plan_id  int;
  v_req_id        bigint;
  v_sub_id        bigint;
  v_now           timestamptz := NOW();
BEGIN
  -- Resolve IDs
  SELECT member_id  INTO v_member2_id  FROM members   WHERE email       = 'existingmember-a@test.cdns';
  SELECT company_id INTO v_co1_id      FROM companies WHERE tin_number  = '100000001';
  SELECT company_id INTO v_co2_id      FROM companies WHERE tin_number  = '100000002';
  SELECT cmm.member_id INTO v_master_a_id
    FROM company_member_mappings cmm
    JOIN companies c ON c.company_id = cmm.company_id
   WHERE c.tin_number = '100000001' AND NOT cmm.is_deleted LIMIT 1;
  SELECT cmm.member_id INTO v_master_b_id
    FROM company_member_mappings cmm
    JOIN companies c ON c.company_id = cmm.company_id
   WHERE c.tin_number = '100000002' AND NOT cmm.is_deleted LIMIT 1;

  -- Look up existing plan IDs (created by MemberPortalSeedData)
  SELECT subscription_id INTO v_decl_plan_id FROM subscriptions
   WHERE subscription_name = 'Declarant Annual' AND subscription_action_type_enum = 0;
  SELECT subscription_id INTO v_cons_plan_id FROM subscriptions
   WHERE subscription_name = 'Consignee Annual' AND subscription_action_type_enum = 0;

  -- ── 7a: New plan: Declarant OnlyForAcknowledgement ────────────────────────
  -- subscription_action_type_enum = 1 (OnlyForAcknowledgement)
  -- subscription_type_enum        = 1 (Declarant)
  -- subscription_status_enum      = 1 (Active)
  INSERT INTO subscriptions (
    subscription_name, description, amount,
    no_of_active_duration, subscription_duration_type_enum,
    subscription_type_enum, subscription_status_enum,
    subscription_action_type_enum, is_deleted, created_date_time
  ) VALUES (
    'Declarant Annual (Acknowledgement Only)',
    'Annual declarant subscription for acknowledgement-only access.',
    25000.0000,
    12, 1,   -- 12 months
    1,  1,   -- Declarant, Active
    1, false, v_now
  )
  RETURNING subscription_id INTO v_ack_plan_id;

  -- ── 7b: member 2 + company 1 + Declarant OnlyForAcknowledgement ──────────
  -- subscription_request shape: approved by master-a
  INSERT INTO subscription_requests (
    member_id, company_id, subscription_type_enum,
    subscription_request_status_enum,
    requested_date_time, respond_date_time, responder_id,
    responder_comment, global_identity, is_deleted
  ) VALUES (
    v_member2_id, v_co1_id, 1,
    1,  -- Approved
    v_now - interval '100 days', v_now - interval '99 days', v_master_a_id,
    'Approved.', gen_random_uuid(), false
  )
  RETURNING subscription_request_id INTO v_req_id;

  UPDATE subscription_requests
     SET subscription_request_number = 'SR-' || lpad(v_req_id::text, 6, '0')
   WHERE subscription_request_id = v_req_id;

  INSERT INTO member_subscriptions (
    subscription_request_id, member_id, subscription_id,
    company_id, company_master_member_id,
    start_date, expiry_date,
    paid_amount, paid_date, transaction_reference_number,
    member_subscription_status_enum, subscription_amount,
    no_of_active_duration, subscription_duration_type_enum,
    subscription_name, subscription_type_enum,
    subscription_action_type_enum, global_identity, key
  ) VALUES (
    v_req_id, v_member2_id, v_ack_plan_id,
    v_co1_id, v_master_a_id,
    v_now - interval '100 days', v_now + interval '265 days',
    0.00, NULL, '',
    1,  -- Activate
    25000.00,
    12, 1,  -- 12 months
    'Declarant Annual (Acknowledgement Only)', 1,
    1,  -- OnlyForAcknowledgement
    gen_random_uuid(), NULL
  )
  RETURNING member_subscription_id INTO v_sub_id;

  UPDATE member_subscriptions
     SET member_subscription_number = 'MS-' || lpad(v_sub_id::text, 6, '0')
   WHERE member_subscription_id = v_sub_id;

  -- ── 7c: master-a self-subscription  (Declarant, company 1) ───────────────
  -- Shaped like ApproveRequestAsync for a self-request (MemberId == ResponderId).
  -- Uses "Declarant Annual" plan (subscription_action_type_enum = 0, General).
  INSERT INTO subscription_requests (
    member_id, company_id, subscription_type_enum,
    subscription_request_status_enum,
    requested_date_time, respond_date_time, responder_id,
    responder_comment, global_identity, is_deleted
  ) VALUES (
    v_master_a_id, v_co1_id, 1,
    1,  -- Approved
    v_now - interval '180 days', v_now - interval '179 days', v_master_a_id,
    'Approved.', gen_random_uuid(), false
  )
  RETURNING subscription_request_id INTO v_req_id;

  UPDATE subscription_requests
     SET subscription_request_number = 'SR-' || lpad(v_req_id::text, 6, '0')
   WHERE subscription_request_id = v_req_id;

  INSERT INTO member_subscriptions (
    subscription_request_id, member_id, subscription_id,
    company_id, company_master_member_id,
    start_date, expiry_date,
    paid_amount, paid_date, transaction_reference_number,
    member_subscription_status_enum, subscription_amount,
    no_of_active_duration, subscription_duration_type_enum,
    subscription_name, subscription_type_enum,
    subscription_action_type_enum, global_identity, key
  ) VALUES (
    v_req_id, v_master_a_id, v_decl_plan_id,
    v_co1_id, v_master_a_id,
    v_now - interval '180 days', v_now + interval '185 days',
    0.00, NULL, '',
    1,  -- Activate
    25000.00,
    12, 1,
    'Declarant Annual', 1,
    0,  -- General
    gen_random_uuid(), NULL
  )
  RETURNING member_subscription_id INTO v_sub_id;

  UPDATE member_subscriptions
     SET member_subscription_number = 'MS-' || lpad(v_sub_id::text, 6, '0')
   WHERE member_subscription_id = v_sub_id;

  -- ── 7d: master-b self-subscription  (Consignee, company 2) ───────────────
  -- Uses "Consignee Annual" plan (subscription_action_type_enum = 0, General).
  INSERT INTO subscription_requests (
    member_id, company_id, subscription_type_enum,
    subscription_request_status_enum,
    requested_date_time, respond_date_time, responder_id,
    responder_comment, global_identity, is_deleted
  ) VALUES (
    v_master_b_id, v_co2_id, 2,
    1,  -- Approved
    v_now - interval '150 days', v_now - interval '149 days', v_master_b_id,
    'Approved.', gen_random_uuid(), false
  )
  RETURNING subscription_request_id INTO v_req_id;

  UPDATE subscription_requests
     SET subscription_request_number = 'SR-' || lpad(v_req_id::text, 6, '0')
   WHERE subscription_request_id = v_req_id;

  INSERT INTO member_subscriptions (
    subscription_request_id, member_id, subscription_id,
    company_id, company_master_member_id,
    start_date, expiry_date,
    paid_amount, paid_date, transaction_reference_number,
    member_subscription_status_enum, subscription_amount,
    no_of_active_duration, subscription_duration_type_enum,
    subscription_name, subscription_type_enum,
    subscription_action_type_enum, global_identity, key
  ) VALUES (
    v_req_id, v_master_b_id, v_cons_plan_id,
    v_co2_id, v_master_b_id,
    v_now - interval '150 days', v_now + interval '215 days',
    0.00, NULL, '',
    1,  -- Activate
    18000.00,
    12, 1,
    'Consignee Annual', 2,
    0,  -- General
    gen_random_uuid(), NULL
  )
  RETURNING member_subscription_id INTO v_sub_id;

  UPDATE member_subscriptions
     SET member_subscription_number = 'MS-' || lpad(v_sub_id::text, 6, '0')
   WHERE member_subscription_id = v_sub_id;

END;
$$;


COMMIT;
