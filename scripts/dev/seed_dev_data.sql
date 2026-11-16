-- =============================================================================
-- Dev seed supplement — adds what MemberPortalSeedData does NOT cover.
--
-- What AuthSeedData + MemberPortalSeedData already provide:
--   • Company A (TIN 100000001) + Master A (master-a@test.cdns)
--   • Company B (TIN 100000002) + Master B (master-b@test.cdns)
--   • Member A (existingmember-a@test.cdns) — the "logged-in" test member
--   • 3 subscription plans: Declarant Annual, Consignee Annual, Declarant Monthly
--   • Active Declarant subscription (12 months, General, Company A)
--   • Active Consignee subscription (12 months, General, Company B)
--   • 1 expired subscription (Declarant, Company A)
--   • Requests: Pending × 2, Approved × 3, Rejected × 1, Cancelled × 1
--     (request numbers use a legacy format, not SR-{id:D6})
--
-- What THIS script adds (idempotent, transaction-wrapped):
--   • CompanyMemberMapping: member A → Company A (if not already present)
--   • Active Consignee subscription with SubscriptionActionTypeEnum=1 (OnlyForAcknowledgement)
--   • Deactivated subscription (MemberSubscriptionStatusEnum=2)
--   • One new Pending, Approved, Rejected (with responder comment), Cancelled request
--     with correct SR-{id:D6} numbers
--
-- Enum values used (matches Cdns.Domain/Enums):
--   SubscriptionTypeEnum:         Declarant=1, Consignee=2
--   SubscriptionActionTypeEnum:   General=0, OnlyForAcknowledgement=1
--   SubscriptionDurationTypeEnum: Days=0, Months=1, Years=2
--   SubscriptionStatusEnum:       Draft=0, Active=1, Inactive=2
--   MemberSubscriptionStatusEnum: PaymentPending=0, Activate=1, Deactivate=2,
--                                 Suspended=3, Expired=4, PaymentInprogress=5,
--                                 ChangeSubscriptionType=6
--   SubscriptionRequestStatusEnum:Pending=0, Approved=1, Rejected=2, Cancelled=3
-- =============================================================================

BEGIN;

DO $$
DECLARE
    v_member_id              BIGINT;
    v_company_a_id           INT;
    v_company_b_id           INT;
    v_master_a_id            BIGINT;
    v_plan_consignee_id      INT;
    v_plan_declarant_id      INT;
    v_req_id                 BIGINT;
    v_req_approved_id        BIGINT;
    v_req_ack_id             BIGINT;
    v_req_deact_id           BIGINT;
    v_req_pending_id         BIGINT;
    v_req_rejected_id        BIGINT;
    v_req_cancelled_id       BIGINT;
BEGIN

    -- -------------------------------------------------------------------------
    -- 1. Resolve prerequisite IDs from AuthSeedData rows
    -- -------------------------------------------------------------------------
    SELECT member_id INTO v_member_id
    FROM members WHERE email = 'existingmember-a@test.cdns';
    IF v_member_id IS NULL THEN
        RAISE EXCEPTION 'AuthSeedData has not been applied — member existingmember-a@test.cdns not found';
    END IF;

    SELECT company_id INTO v_company_a_id FROM companies WHERE tin_number = '100000001';
    SELECT company_id INTO v_company_b_id FROM companies WHERE tin_number = '100000002';

    -- Master A is the first (and only) mapping on Company A from AuthSeedData
    SELECT member_id INTO v_master_a_id
    FROM company_member_mappings
    WHERE company_id = v_company_a_id
    ORDER BY company_member_mapping_id
    LIMIT 1;

    -- Plans created by MemberPortalSeedData
    SELECT subscription_id INTO v_plan_consignee_id
    FROM subscriptions WHERE subscription_name = 'Consignee Annual' AND is_deleted = false LIMIT 1;

    SELECT subscription_id INTO v_plan_declarant_id
    FROM subscriptions WHERE subscription_name = 'Declarant Annual' AND is_deleted = false LIMIT 1;

    IF v_plan_consignee_id IS NULL OR v_plan_declarant_id IS NULL THEN
        RAISE EXCEPTION 'MemberPortalSeedData has not been applied — subscription plans not found';
    END IF;

    -- -------------------------------------------------------------------------
    -- 2. Map member A to Company A (required for notification fan-out)
    -- -------------------------------------------------------------------------
    INSERT INTO company_member_mappings (company_id, member_id, is_deleted)
    SELECT v_company_a_id, v_member_id, false
    WHERE NOT EXISTS (
        SELECT 1 FROM company_member_mappings
        WHERE company_id = v_company_a_id AND member_id = v_member_id
    );

    -- -------------------------------------------------------------------------
    -- 3. Active Consignee subscription — OnlyForAcknowledgement (action=1)
    --    Backed by its own Approved request with an SR- number.
    -- -------------------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1 FROM member_subscriptions
        WHERE member_id = v_member_id
          AND subscription_type_enum = 2        -- Consignee
          AND subscription_action_type_enum = 1 -- OnlyForAcknowledgement
          AND member_subscription_status_enum = 1 -- Activate
    ) THEN
        INSERT INTO subscription_requests (
            member_id, company_id, requested_date_time, respond_date_time,
            responder_id, comment, responder_comment,
            subscription_request_status_enum, subscription_type_enum,
            global_identity, is_deleted
        ) VALUES (
            v_member_id, v_company_a_id, NOW() - INTERVAL '100 days', NOW() - INTERVAL '99 days',
            v_master_a_id, 'Consignee OnlyForAcknowledgement request.', 'Approved.',
            1, -- Approved
            2, -- Consignee
            gen_random_uuid(), false
        ) RETURNING subscription_request_id INTO v_req_ack_id;

        UPDATE subscription_requests
        SET subscription_request_number = 'SR-' || lpad(v_req_ack_id::text, 6, '0')
        WHERE subscription_request_id = v_req_ack_id;

        INSERT INTO member_subscriptions (
            subscription_request_id, member_id, subscription_id,
            company_id, company_master_member_id,
            start_date, expiry_date,
            paid_amount, paid_date, subscription_amount,
            member_subscription_status_enum,
            no_of_active_duration, subscription_duration_type_enum,
            subscription_name, transaction_reference_number,
            subscription_type_enum, subscription_action_type_enum,
            global_identity
        )
        SELECT
            v_req_ack_id, v_member_id, v_plan_consignee_id,
            v_company_a_id, v_master_a_id,
            NOW() - INTERVAL '100 days', NOW() + INTERVAL '265 days',
            amount, NOW() - INTERVAL '100 days', amount,
            1,  -- Activate
            no_of_active_duration, subscription_duration_type_enum,
            subscription_name,
            'TXN-MS-ACK-001',
            2,  -- Consignee
            1,  -- OnlyForAcknowledgement
            gen_random_uuid()
        FROM subscriptions WHERE subscription_id = v_plan_consignee_id;
    END IF;

    -- -------------------------------------------------------------------------
    -- 4. Deactivated subscription (MemberSubscriptionStatusEnum=2)
    -- -------------------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1 FROM member_subscriptions
        WHERE member_id = v_member_id
          AND member_subscription_status_enum = 2 -- Deactivate
    ) THEN
        INSERT INTO subscription_requests (
            member_id, company_id, requested_date_time, respond_date_time,
            responder_id, comment, responder_comment,
            subscription_request_status_enum, subscription_type_enum,
            global_identity, is_deleted
        ) VALUES (
            v_member_id, v_company_b_id, NOW() - INTERVAL '50 days', NOW() - INTERVAL '49 days',
            v_master_a_id, 'Deactivated subscription request.', 'Approved then deactivated.',
            1, -- Approved
            1, -- Declarant
            gen_random_uuid(), false
        ) RETURNING subscription_request_id INTO v_req_deact_id;

        UPDATE subscription_requests
        SET subscription_request_number = 'SR-' || lpad(v_req_deact_id::text, 6, '0')
        WHERE subscription_request_id = v_req_deact_id;

        INSERT INTO member_subscriptions (
            subscription_request_id, member_id, subscription_id,
            company_id, company_master_member_id,
            start_date, expiry_date,
            paid_amount, paid_date, subscription_amount,
            member_subscription_status_enum,
            no_of_active_duration, subscription_duration_type_enum,
            subscription_name, transaction_reference_number,
            subscription_type_enum, subscription_action_type_enum,
            global_identity
        )
        SELECT
            v_req_deact_id, v_member_id, v_plan_declarant_id,
            v_company_b_id, v_master_a_id,
            NOW() - INTERVAL '50 days', NOW() + INTERVAL '315 days',
            amount, NOW() - INTERVAL '50 days', amount,
            2,  -- Deactivate
            no_of_active_duration, subscription_duration_type_enum,
            subscription_name,
            'TXN-MS-DEACT-001',
            1,  -- Declarant
            0,  -- General
            gen_random_uuid()
        FROM subscriptions WHERE subscription_id = v_plan_declarant_id;
    END IF;

    -- -------------------------------------------------------------------------
    -- 5. Four requests with correct SR-{id:D6} numbers
    --    (supplement the seed's legacy-formatted requests)
    -- -------------------------------------------------------------------------

    -- 5a. Pending
    IF NOT EXISTS (
        SELECT 1 FROM subscription_requests
        WHERE member_id = v_member_id
          AND subscription_request_status_enum = 0  -- Pending
          AND subscription_request_number LIKE 'SR-%'
          AND char_length(subscription_request_number) = 9  -- SR-nnnnnn
    ) THEN
        INSERT INTO subscription_requests (
            member_id, company_id, requested_date_time,
            subscription_request_status_enum, subscription_type_enum,
            comment, global_identity, is_deleted
        ) VALUES (
            v_member_id, v_company_a_id, NOW() - INTERVAL '1 day',
            0, 1,  -- Pending, Declarant
            'Awaiting master approval (SR script).', gen_random_uuid(), false
        ) RETURNING subscription_request_id INTO v_req_pending_id;

        UPDATE subscription_requests
        SET subscription_request_number = 'SR-' || lpad(v_req_pending_id::text, 6, '0')
        WHERE subscription_request_id = v_req_pending_id;
    END IF;

    -- 5b. Approved (standalone, no member_subscription row)
    INSERT INTO subscription_requests (
        member_id, company_id, requested_date_time, respond_date_time,
        responder_id, comment, responder_comment,
        subscription_request_status_enum, subscription_type_enum,
        global_identity, is_deleted
    ) VALUES (
        v_member_id, v_company_b_id, NOW() - INTERVAL '10 days', NOW() - INTERVAL '9 days',
        v_master_a_id, 'Approved request (SR script).', 'Approved.',
        1, 2,  -- Approved, Consignee
        gen_random_uuid(), false
    ) RETURNING subscription_request_id INTO v_req_approved_id;

    UPDATE subscription_requests
    SET subscription_request_number = 'SR-' || lpad(v_req_approved_id::text, 6, '0')
    WHERE subscription_request_id = v_req_approved_id;

    -- 5c. Rejected (with responder comment)
    INSERT INTO subscription_requests (
        member_id, company_id, requested_date_time, respond_date_time,
        responder_id, comment, responder_comment,
        subscription_request_status_enum, subscription_type_enum,
        global_identity, is_deleted
    ) VALUES (
        v_member_id, v_company_a_id, NOW() - INTERVAL '15 days', NOW() - INTERVAL '14 days',
        v_master_a_id, 'Rejected request (SR script).', 'Does not meet current requirements.',
        2, 1,  -- Rejected, Declarant
        gen_random_uuid(), false
    ) RETURNING subscription_request_id INTO v_req_rejected_id;

    UPDATE subscription_requests
    SET subscription_request_number = 'SR-' || lpad(v_req_rejected_id::text, 6, '0')
    WHERE subscription_request_id = v_req_rejected_id;

    -- 5d. Cancelled
    INSERT INTO subscription_requests (
        member_id, company_id, requested_date_time,
        subscription_request_status_enum, subscription_type_enum,
        comment, global_identity, is_deleted
    ) VALUES (
        v_member_id, v_company_b_id, NOW() - INTERVAL '20 days',
        3, 2,  -- Cancelled, Consignee
        'Cancelled by member (SR script).', gen_random_uuid(), false
    ) RETURNING subscription_request_id INTO v_req_cancelled_id;

    UPDATE subscription_requests
    SET subscription_request_number = 'SR-' || lpad(v_req_cancelled_id::text, 6, '0')
    WHERE subscription_request_id = v_req_cancelled_id;

    RAISE NOTICE 'Dev seed supplement applied. New request IDs: pending=%, approved=%, rejected=%, cancelled=%',
        v_req_pending_id, v_req_approved_id, v_req_rejected_id, v_req_cancelled_id;

END $$;

COMMIT;
