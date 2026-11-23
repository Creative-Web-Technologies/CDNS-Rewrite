-- =============================================================================
-- cusdec-ui-seed-cleanup.sql
-- Removes exactly the rows inserted by cusdec-ui-seed.sql.
-- Safe to run on a DB that never had the seed applied (all DELETEs become no-ops).
-- Children are deleted before parents to satisfy FK RESTRICT constraints.
-- =============================================================================

BEGIN;

-- =============================================================================
-- SECTION A: CusDec data  (cus_dec_external_system_number range 960000001+)
-- Delete in FK order: statuses → containers/items → parent cus_decs
-- =============================================================================

-- A1: status rows (FK to cus_decs: RESTRICT)
DELETE FROM cus_dec_statuses
WHERE cus_dec_external_system_number >= 960000001;

-- A2: container statuses  (FK to cus_dec_containers: RESTRICT)
-- No containers were seeded, but included for correctness.
DELETE FROM cus_dec_container_statuses
WHERE cus_dec_external_system_number >= 960000001;

-- A3: containers  (FK to cus_decs: RESTRICT)
DELETE FROM cus_dec_containers
WHERE cus_dec_external_system_number >= 960000001;

-- A4: items  (FK to cus_decs: RESTRICT)
DELETE FROM cus_dec_items
WHERE cus_dec_external_system_number >= 960000001;

-- A5: parent declarations
DELETE FROM cus_decs
WHERE cus_dec_external_system_number >= 960000001;


-- =============================================================================
-- SECTION B: Hidden status mapping  (code 600)
-- =============================================================================
DELETE FROM cus_dec_status_mappings
WHERE asycuda_status = '600'
  AND cdns_status    = 'Internal check';


-- =============================================================================
-- SECTION C: Subscriptions
-- Deletion order:
--   C1. Capture subscription_request_ids before any member_subscription rows vanish
--   C2. Delete member_subscriptions for master-a and master-b (no cascade trigger)
--   C3. Delete the new ack-only plan → cascades to delete member 2's member_subscription
--   C4. Delete the three subscription_requests using the captured IDs
-- =============================================================================

-- C1: Capture subscription_request_ids while member_subscriptions still exist.
--     Covers all three seeded subscriptions:
--       • member 2 + ack-only plan
--       • master-a self-subscription
--       • master-b self-subscription
CREATE TEMP TABLE _del_req_ids AS
SELECT ms.subscription_request_id
FROM member_subscriptions ms
WHERE ms.subscription_id = (
  SELECT subscription_id FROM subscriptions
  WHERE subscription_name = 'Declarant Annual (Acknowledgement Only)'
)
UNION
SELECT ms.subscription_request_id
FROM member_subscriptions ms
WHERE ms.member_id IN (
  SELECT cmm.member_id
    FROM company_member_mappings cmm
    JOIN companies c ON c.company_id = cmm.company_id
   WHERE c.tin_number IN ('100000001', '100000002')
     AND NOT cmm.is_deleted
);

-- C2: Delete master-a and master-b member_subscriptions explicitly.
--     These reference subscription_requests (FK RESTRICT on the request side),
--     so they must go first before we can delete the requests.
DELETE FROM member_subscriptions
WHERE member_id IN (
  SELECT cmm.member_id
    FROM company_member_mappings cmm
    JOIN companies c ON c.company_id = cmm.company_id
   WHERE c.tin_number IN ('100000001', '100000002')
     AND NOT cmm.is_deleted
);

-- C3: Delete the ack-only plan.
--     MemberSubscriptionConfiguration sets OnDelete.Cascade for the Subscription FK,
--     so this automatically removes member 2's member_subscription row.
DELETE FROM subscriptions
WHERE subscription_name = 'Declarant Annual (Acknowledgement Only)';

-- C4: Delete the three subscription_requests (now that their children are gone).
DELETE FROM subscription_requests
WHERE subscription_request_id IN (
  SELECT subscription_request_id FROM _del_req_ids
);


COMMIT;
