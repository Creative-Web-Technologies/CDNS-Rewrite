-- check_tin_signup_status(p_tin)
--
-- Checks whether a company TIN is in a state that allows new member signup.
-- Implemented as a Postgres function rather than C# because the check is a
-- genuine 4-table join (companies → company_member_mappings → members →
-- member_users) that is cleaner and atomic when expressed as set-based SQL.
--
-- Return codes
--   1  company found, master active                  → new member may proceed
--   2  company found, master pending eReg            → block signup
--   3  company found, master pending activation      → block signup
--   4  TIN is shorter than 9 characters              → validation error
--   5  company found, master account incomplete      → block signup
--   6  company not found in DB                       → block signup

CREATE OR REPLACE FUNCTION check_tin_signup_status(p_tin text)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
  v_company_id  integer;
  v_member_id   bigint;
  v_user_status integer;
  v_req_ereg    boolean;
BEGIN
  -- code 4: TIN too short
  IF length(trim(p_tin)) < 9 THEN
    RETURN 4;
  END IF;

  -- code 6: company not in DB
  SELECT company_id
  INTO   v_company_id
  FROM   companies
  WHERE  tin_number = p_tin
    AND  is_deleted  = false;

  IF NOT FOUND THEN
    RETURN 6;
  END IF;

  -- find the CompanyMasterMember for this company
  SELECT m.member_id
  INTO   v_member_id
  FROM   company_member_mappings cmm
  JOIN   members m ON m.member_id = cmm.member_id
  WHERE  cmm.company_id = v_company_id
    AND  m.member_type   = 2        -- CompanyMasterMember
    AND  cmm.is_deleted  = false
    AND  m.is_deleted    = false
  LIMIT  1;

  -- code 5: master Member row exists but no MemberUser yet (or no master linked)
  IF NOT FOUND THEN
    RETURN 5;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM member_users WHERE member_id = v_member_id) THEN
    RETURN 5;
  END IF;

  -- inspect the master's MemberUser
  SELECT user_status_type, required_ereg_verification
  INTO   v_user_status, v_req_ereg
  FROM   member_users
  WHERE  member_id = v_member_id;

  -- code 1: master is active → company verified, new member may register
  IF v_user_status = 1 THEN
    RETURN 1;
  END IF;

  -- code 2: master account exists but awaiting eReg verification
  IF v_req_ereg = true THEN
    RETURN 2;
  END IF;

  -- code 3: master account exists but not yet activated
  RETURN 3;
END;
$$;
