using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Subscriptions;

public sealed class MasterSubscriptionRequestService : IMasterSubscriptionRequestService
{
    private readonly CdnsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBusinessClock _clock;
    private readonly IMemberSubscriptionKeyService _keyService;

    public MasterSubscriptionRequestService(
        CdnsDbContext db,
        ICurrentUser currentUser,
        IBusinessClock clock,
        IMemberSubscriptionKeyService keyService)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _keyService = keyService;
    }

    // ── L1: List all requests ─────────────────────────────────────────────────

    public async Task<PagedResult<MasterSubscriptionRequestSummaryDto>> GetRequestsAsync(
        int? status,
        string? requestNumber,
        string? memberEmail,
        string? search,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => await QueryRequestsAsync(
            status, requestNumber, memberEmail, search, fromDate, toDate, page, pageSize, ct);

    // ── L2: Pending-only ──────────────────────────────────────────────────────

    public async Task<PagedResult<MasterSubscriptionRequestSummaryDto>> GetPendingRequestsAsync(
        string? requestNumber,
        string? memberEmail,
        string? search,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => await QueryRequestsAsync(
            (int)SubscriptionRequestStatusEnum.Pending,
            requestNumber, memberEmail, search, fromDate, toDate, page, pageSize, ct);

    // ── L3: View request detail ───────────────────────────────────────────────

    public async Task<MasterSubscriptionRequestDetailDto?> GetRequestAsync(
        long requestId, CancellationToken ct = default)
    {
        var masterId = _currentUser.MemberId;
        var companyIds = await MasterCompanyIdsAsync(masterId, ct);
        if (companyIds.Count == 0) return null;

        var row = await (
            from r in _db.SubscriptionRequests
            where r.SubscriptionRequestId == requestId
                  && r.IsDeleted != true
                  && r.CompanyId != null
                  && companyIds.Contains(r.CompanyId.Value)
                  && r.MemberId != masterId
            join m in _db.Members on r.MemberId equals m.MemberId
            join c in _db.Companies on r.CompanyId equals c.CompanyId into cj
            from c in cj.DefaultIfEmpty()
            select new { r, m, c }
        ).FirstOrDefaultAsync(ct);

        if (row is null) return null;

        string? responderName = null;
        if (row.r.ResponderId.HasValue)
        {
            responderName = await _db.Members
                .Where(m => m.MemberId == row.r.ResponderId.Value && !m.IsDeleted)
                .Select(m => ((m.FirstName ?? "") + " " + (m.LastName ?? "")).Trim())
                .FirstOrDefaultAsync(ct);
        }

        return new MasterSubscriptionRequestDetailDto(
            row.r.SubscriptionRequestId,
            row.r.SubscriptionRequestNumber,
            row.r.RequestedDateTime,
            MemberName(row.m),
            row.m.Email ?? "—",
            row.c != null ? row.c.CompanyName : "—",
            row.c?.TinNumber,
            row.r.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant ? "Declarant" : "Consignee",
            RequestStatusLabel(row.r.SubscriptionRequestStatusEnum ?? 0),
            row.r.Comment,
            row.r.ResponderComment,
            row.r.RespondDateTime,
            string.IsNullOrWhiteSpace(responderName) ? null : responderName
        );
    }

    // ── L4: Reject ────────────────────────────────────────────────────────────

    public async Task<RejectRequestResult> RejectRequestAsync(
        long requestId, string comment, CancellationToken ct = default)
    {
        var masterId = _currentUser.MemberId;
        var companyIds = await MasterCompanyIdsAsync(masterId, ct);
        if (companyIds.Count == 0) return new RejectRequestResult.NotFound();

        var request = await _db.SubscriptionRequests
            .FirstOrDefaultAsync(r =>
                r.SubscriptionRequestId == requestId
                && r.IsDeleted != true
                && r.CompanyId != null
                && companyIds.Contains(r.CompanyId.Value)
                && r.MemberId != masterId, ct);

        if (request is null) return new RejectRequestResult.NotFound();
        if (request.SubscriptionRequestStatusEnum != (int)SubscriptionRequestStatusEnum.Pending)
            return new RejectRequestResult.NotPending();

        request.SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Rejected;
        request.ResponderComment = comment;
        request.RespondDateTime = _clock.Now();
        request.ResponderId = masterId;

        _db.MemberNotifications.Add(new MemberNotification
        {
            MemberId = request.MemberId,
            Title = "Subscription Rejected.",
            Message = "Your subscription request was rejected by the company administrator.",
            NotificationDateTime = _clock.Now(),
            IsSeen = false,
            ShowOnHome = true,
            IsDeleted = false,
            NotificationType = NotificationType.Danger,
        });

        await _db.SaveChangesAsync(ct);
        return new RejectRequestResult.Success();
    }

    // ── L5: Plans for plan picker ─────────────────────────────────────────────
    // Own request (MemberId == masterId): all active plans, both types.
    // Member's request: plans filtered to request's subscription type.

    public async Task<IReadOnlyList<SubscriptionPlanDto>?> GetPlansForRequestAsync(
        long requestId, CancellationToken ct = default)
    {
        var masterId = _currentUser.MemberId;
        var companyIds = await MasterCompanyIdsAsync(masterId, ct);
        if (companyIds.Count == 0) return null;

        var request = await _db.SubscriptionRequests
            .FirstOrDefaultAsync(r =>
                r.SubscriptionRequestId == requestId
                && r.IsDeleted != true
                && r.CompanyId != null
                && companyIds.Contains(r.CompanyId.Value), ct);

        if (request is null) return null;

        if (request.SubscriptionRequestStatusEnum != (int)SubscriptionRequestStatusEnum.Pending)
            return [];

        bool isSelf = request.MemberId == masterId;

        var plansQuery = _db.Subscriptions
            .Where(s => !s.IsDeleted && s.SubscriptionStatusEnum == (int)SubscriptionStatusEnum.Active);

        if (!isSelf)
            plansQuery = plansQuery.Where(s => s.SubscriptionTypeEnum == request.SubscriptionTypeEnum);

        var plans = await plansQuery
            .Select(s => new SubscriptionPlanDto(
                s.SubscriptionId,
                s.SubscriptionName,
                s.Description,
                s.Amount,
                s.NoOfActiveDuration,
                DurationLabel(s.SubscriptionDurationTypeEnum),
                s.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant ? "Declarant" : "Consignee",
                s.SubscriptionActionTypeEnum == (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement
                    ? "AcknowledgementOnly" : "General"
            ))
            .ToListAsync(ct);

        return plans;
    }

    // ── L6: Approve ───────────────────────────────────────────────────────────
    // Own request: any plan type allowed; request type updated to match plan.
    // Member's request: plan type must match request type (TypeMismatch otherwise).

    public async Task<ApproveRequestResult> ApproveRequestAsync(
        long requestId, int planId, CancellationToken ct = default)
    {
        var masterId = _currentUser.MemberId;
        var companyIds = await MasterCompanyIdsAsync(masterId, ct);
        if (companyIds.Count == 0) return new ApproveRequestResult.NotFound();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var request = await _db.SubscriptionRequests
            .FirstOrDefaultAsync(r =>
                r.SubscriptionRequestId == requestId
                && r.IsDeleted != true
                && r.CompanyId != null
                && companyIds.Contains(r.CompanyId.Value), ct);

        if (request is null) return new ApproveRequestResult.NotFound();

        if (request.SubscriptionRequestStatusEnum != (int)SubscriptionRequestStatusEnum.Pending)
            return new ApproveRequestResult.NotPending();

        bool isSelf = request.MemberId == masterId;

        var plan = await _db.Subscriptions
            .FirstOrDefaultAsync(s =>
                s.SubscriptionId == planId
                && !s.IsDeleted
                && s.SubscriptionStatusEnum == (int)SubscriptionStatusEnum.Active, ct);

        if (plan is null) return new ApproveRequestResult.PlanNotFound();

        if (!isSelf && plan.SubscriptionTypeEnum != request.SubscriptionTypeEnum)
            return new ApproveRequestResult.TypeMismatch();

        var now = _clock.Now();
        // ComputeExpiryDate returns a Colombo-offset value; convert to UTC for storage.
        var expiryDate = SubscriptionExpiry.ComputeExpiryDate(
            now, plan.NoOfActiveDuration, plan.SubscriptionDurationTypeEnum);

        var subscription = new MemberSubscription
        {
            SubscriptionRequestId = requestId,
            MemberId = request.MemberId,
            SubscriptionId = plan.SubscriptionId,
            CompanyId = request.CompanyId!.Value,
            CompanyMasterMemberId = masterId,
            StartDate = now,
            ExpiryDate = expiryDate.ToUniversalTime(),
            PaidAmount = 0m,
            PaidDate = null,
            TransactionReferenceNumber = "",
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            SubscriptionAmount = plan.Amount,
            NoOfActiveDuration = plan.NoOfActiveDuration,
            SubscriptionDurationTypeEnum = plan.SubscriptionDurationTypeEnum,
            SubscriptionName = plan.SubscriptionName,
            SubscriptionTypeEnum = plan.SubscriptionTypeEnum ?? request.SubscriptionTypeEnum ?? (int)SubscriptionTypeEnum.Consignee,
            SubscriptionActionTypeEnum = plan.SubscriptionActionTypeEnum,
            GlobalIdentity = Guid.NewGuid(),
        };

        _db.MemberSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(ct);

        // Set number and HMAC key after ID is known.
        // Generate uses ToColomboDayString internally, so passing the original
        // Colombo-offset expiryDate or its UTC equivalent both yield the same date.
        subscription.MemberSubscriptionNumber = $"MS-{subscription.MemberSubscriptionId:D6}";
        subscription.Key = _keyService.Generate(
            subscription.MemberId,
            subscription.SubscriptionId,
            subscription.MemberSubscriptionId,
            expiryDate,
            subscription.PaidAmount,
            subscription.SubscriptionActionTypeEnum,
            subscription.SubscriptionTypeEnum);

        // Approve the request; for own request sync the type to match the chosen plan.
        if (isSelf)
            request.SubscriptionTypeEnum = plan.SubscriptionTypeEnum;
        request.SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved;
        request.RespondDateTime = now;
        request.ResponderId = masterId;

        _db.MemberNotifications.Add(new MemberNotification
        {
            MemberId = request.MemberId,
            Title = "Subscription Approved.",
            Message = "Your subscription request was successfully approved by the company administrator.",
            NotificationDateTime = now,
            IsSeen = false,
            ShowOnHome = true,
            IsDeleted = false,
            NotificationType = NotificationType.Success,
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new ApproveRequestResult.Success(
            subscription.MemberSubscriptionId,
            subscription.MemberSubscriptionNumber);
    }

    // ── L7: Master self-create (no body) ─────────────────────────────────────
    // Picks the master's first mapped company (ordered by ID). Type defaults to
    // Consignee so the plan picker pre-selects that tab; it is updated to the
    // chosen plan's type on approve.

    public async Task<CreateMasterRequestResult> CreateSelfRequestAsync(
        CancellationToken ct = default)
    {
        var masterId = _currentUser.MemberId;
        var companyIds = await MasterCompanyIdsAsync(masterId, ct);
        if (companyIds.Count == 0)
            return new CreateMasterRequestResult.CompanyNotMapped();

        var companyId = companyIds.OrderBy(id => id).First();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var request = new SubscriptionRequest
        {
            MemberId = masterId,
            CompanyId = companyId,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Consignee,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = _clock.Now(),
            IsDeleted = false,
            GlobalIdentity = Guid.NewGuid(),
        };

        _db.SubscriptionRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        request.SubscriptionRequestNumber = $"SR-{request.SubscriptionRequestId:D6}";
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new CreateMasterRequestResult.Success(
            request.SubscriptionRequestId,
            request.SubscriptionRequestNumber!);
    }

    // ── Shared query ──────────────────────────────────────────────────────────

    private async Task<PagedResult<MasterSubscriptionRequestSummaryDto>> QueryRequestsAsync(
        int? status,
        string? requestNumber,
        string? memberEmail,
        string? search,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var masterId = _currentUser.MemberId;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var companyIds = await MasterCompanyIdsAsync(masterId, ct);
        if (companyIds.Count == 0)
            return new PagedResult<MasterSubscriptionRequestSummaryDto>([], 0, page, pageSize);

        var query = from r in _db.SubscriptionRequests
                    where r.IsDeleted != true
                          && r.CompanyId != null
                          && companyIds.Contains(r.CompanyId.Value)
                          && r.MemberId != masterId
                    join m in _db.Members on r.MemberId equals m.MemberId
                    select new { r, m };

        if (status.HasValue)
            query = query.Where(x => x.r.SubscriptionRequestStatusEnum == status.Value);

        if (!string.IsNullOrWhiteSpace(requestNumber))
        {
            var lower = requestNumber.ToLower();
            query = query.Where(x =>
                x.r.SubscriptionRequestNumber != null &&
                x.r.SubscriptionRequestNumber.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(memberEmail))
        {
            var lower = memberEmail.ToLower();
            query = query.Where(x =>
                x.m.Email != null && x.m.Email.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(x =>
                (x.m.FirstName != null && x.m.FirstName.ToLower().Contains(lower)) ||
                (x.m.LastName  != null && x.m.LastName.ToLower().Contains(lower))  ||
                (x.m.Email     != null && x.m.Email.ToLower().Contains(lower))     ||
                (x.r.SubscriptionRequestNumber != null &&
                 x.r.SubscriptionRequestNumber.ToLower().Contains(lower)));
        }

        if (fromDate.HasValue && toDate.HasValue)
            query = query.Where(x =>
                x.r.RequestedDateTime >= fromDate.Value &&
                x.r.RequestedDateTime <= toDate.Value);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(x => x.r.RequestedDateTime)
            .ThenBy(x => x.r.SubscriptionRequestStatusEnum)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MasterSubscriptionRequestSummaryDto(
                x.r.SubscriptionRequestId,
                x.r.SubscriptionRequestNumber,
                x.r.RequestedDateTime,
                (x.m.FirstName ?? "") + " " + (x.m.LastName ?? ""),
                x.m.Email ?? "—",
                x.r.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant ? "Declarant" : "Consignee",
                RequestStatusLabel(x.r.SubscriptionRequestStatusEnum ?? 0)
            ))
            .ToListAsync(ct);

        return new PagedResult<MasterSubscriptionRequestSummaryDto>(rows, total, page, pageSize);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<List<int>> MasterCompanyIdsAsync(long masterId, CancellationToken ct)
        => await _db.CompanyMemberMappings
            .Where(m => m.MemberId == masterId && !m.IsDeleted)
            .Select(m => m.CompanyId)
            .Distinct()
            .ToListAsync(ct);

    private static string MemberName(Cdns.Domain.Entities.MemberSignupTables.Member m)
        => ((m.FirstName ?? "") + " " + (m.LastName ?? "")).Trim();

    private static string RequestStatusLabel(int status) => status switch
    {
        (int)SubscriptionRequestStatusEnum.Pending   => "Pending",
        (int)SubscriptionRequestStatusEnum.Approved  => "Approved",
        (int)SubscriptionRequestStatusEnum.Rejected  => "Rejected",
        (int)SubscriptionRequestStatusEnum.Cancelled => "Cancelled",
        _ => "Unknown",
    };

    private static string DurationLabel(int? durationType) => durationType switch
    {
        (int)SubscriptionDurationTypeEnum.Months => "Months",
        (int)SubscriptionDurationTypeEnum.Years  => "Years",
        _ => "Days",
    };
}
