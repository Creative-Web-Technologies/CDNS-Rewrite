using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Subscriptions;

public sealed class MemberSubscriptionRequestService : IMemberSubscriptionRequestService
{
    private readonly CdnsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MemberSubscriptionRequestService(CdnsDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<SubscriptionRequestSummaryDto>> GetRequestsAsync(
        int? status, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var memberId = _currentUser.MemberId;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = from r in _db.SubscriptionRequests
                    where r.MemberId == memberId && r.IsDeleted != true
                    join c in _db.Companies on r.CompanyId equals c.CompanyId into cj
                    from c in cj.DefaultIfEmpty()
                    where c == null || !c.IsDeleted
                    select new { r, c };

        if (status.HasValue)
            query = query.Where(x => x.r.SubscriptionRequestStatusEnum == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(x =>
                (x.c != null && x.c.CompanyName.ToLower().Contains(lower)) ||
                (x.r.SubscriptionRequestNumber != null && x.r.SubscriptionRequestNumber.ToLower().Contains(lower)));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(x => x.r.SubscriptionRequestStatusEnum)
            .ThenByDescending(x => x.r.RequestedDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SubscriptionRequestSummaryDto(
                x.r.SubscriptionRequestId,
                x.r.SubscriptionRequestNumber,
                x.r.RequestedDateTime,
                x.c != null ? x.c.CompanyName : "—",
                x.c != null ? x.c.TinNumber : null,
                x.r.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant ? "Declarant" : "Consignee",
                StatusLabel(x.r.SubscriptionRequestStatusEnum ?? 0)
            ))
            .ToListAsync(ct);

        return new PagedResult<SubscriptionRequestSummaryDto>(rows, total, page, pageSize);
    }

    public async Task<SubscriptionRequestDetailDto?> GetRequestAsync(long requestId, CancellationToken ct = default)
    {
        var memberId = _currentUser.MemberId;

        var row = await (
            from r in _db.SubscriptionRequests
            where r.SubscriptionRequestId == requestId
                  && r.MemberId == memberId
                  && r.IsDeleted != true
            join c in _db.Companies on r.CompanyId equals c.CompanyId into cj
            from c in cj.DefaultIfEmpty()
            select new { r, c }
        ).FirstOrDefaultAsync(ct);

        if (row is null) return null;

        string? responderName = null;
        if (row.r.ResponderId.HasValue)
        {
            responderName = await _db.Members
                .Where(m => m.MemberId == row.r.ResponderId.Value && !m.IsDeleted)
                .Select(m => (m.FirstName ?? "") + " " + (m.LastName ?? ""))
                .FirstOrDefaultAsync(ct);
        }

        return new SubscriptionRequestDetailDto(
            row.r.SubscriptionRequestId,
            row.r.SubscriptionRequestNumber,
            row.r.RequestedDateTime,
            row.c != null ? row.c.CompanyName : "—",
            row.c?.TinNumber,
            row.r.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant ? "Declarant" : "Consignee",
            StatusLabel(row.r.SubscriptionRequestStatusEnum ?? 0),
            string.IsNullOrWhiteSpace(responderName) ? null : responderName.Trim(),
            row.r.ResponderComment,
            row.r.RespondDateTime,
            row.r.Comment
        );
    }

    public async Task<CompanyLookupDto?> LookupCompanyByTinAsync(string tin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tin)) return null;
        var prefix = tin.Length >= 9 ? tin[..9] : tin;

        var company = await _db.Companies
            .Where(c => !c.IsDeleted && c.TinNumber.StartsWith(prefix))
            .Select(c => new CompanyLookupDto(c.CompanyId, c.CompanyName, c.TinNumber))
            .FirstOrDefaultAsync(ct);

        return company;
    }

    public async Task<CreateRequestResult> CreateRequestAsync(
        int companyId, int subscriptionType, string? note, CancellationToken ct = default)
    {
        var memberId = _currentUser.MemberId;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Company must exist and not be soft-deleted
        var companyExists = await _db.Companies
            .AnyAsync(c => c.CompanyId == companyId && !c.IsDeleted, ct);
        if (!companyExists)
        {
            await tx.RollbackAsync(ct);
            return new CreateRequestResult.CompanyNotFound();
        }

        // Duplicate pending check
        var duplicate = await _db.SubscriptionRequests.AnyAsync(r =>
            r.MemberId == memberId
            && r.CompanyId == companyId
            && r.SubscriptionTypeEnum == subscriptionType
            && r.SubscriptionRequestStatusEnum == (int)SubscriptionRequestStatusEnum.Pending
            && r.IsDeleted != true, ct);

        if (duplicate)
        {
            await tx.RollbackAsync(ct);
            return new CreateRequestResult.DuplicatePending();
        }

        var request = new Cdns.Domain.Entities.CompanySubscriptionManagement.SubscriptionRequest
        {
            MemberId = memberId,
            CompanyId = companyId,
            SubscriptionTypeEnum = subscriptionType,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            Comment = note,
            IsDeleted = false,
        };

        _db.SubscriptionRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        // Generate request number from the identity ID
        request.SubscriptionRequestNumber = $"SR-{request.SubscriptionRequestId:D6}";
        await _db.SaveChangesAsync(ct);

        // Notify all members of the company
        var companyMemberIds = await _db.CompanyMemberMappings
            .Where(m => m.CompanyId == companyId && !m.IsDeleted)
            .Select(m => m.MemberId)
            .ToListAsync(ct);

        foreach (var recipientMemberId in companyMemberIds)
        {
            _db.MemberNotifications.Add(new MemberNotification
            {
                MemberId = recipientMemberId,
                Title = "New subscription request",
                Message = $"A new subscription request {request.SubscriptionRequestNumber} has been submitted.",
                NotificationDateTime = DateTimeOffset.UtcNow,
                IsSeen = false,
                ShowOnHome = true,
                IsDeleted = false,
                NotificationType = NotificationType.Info,
            });
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new CreateRequestResult.Success(request.SubscriptionRequestId, request.SubscriptionRequestNumber);
    }

    public async Task<CancelRequestResult> CancelRequestAsync(long requestId, CancellationToken ct = default)
    {
        var memberId = _currentUser.MemberId;

        var request = await _db.SubscriptionRequests
            .FirstOrDefaultAsync(r => r.SubscriptionRequestId == requestId && r.IsDeleted != true, ct);

        if (request is null || request.MemberId != memberId)
            return new CancelRequestResult.NotFound();

        if (request.SubscriptionRequestStatusEnum != (int)SubscriptionRequestStatusEnum.Pending)
            return new CancelRequestResult.NotCancellable();

        request.SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Cancelled;
        await _db.SaveChangesAsync(ct);

        return new CancelRequestResult.Success();
    }

    private static string StatusLabel(int status) => status switch
    {
        (int)SubscriptionRequestStatusEnum.Pending   => "Pending",
        (int)SubscriptionRequestStatusEnum.Approved  => "Approved",
        (int)SubscriptionRequestStatusEnum.Rejected  => "Rejected",
        (int)SubscriptionRequestStatusEnum.Cancelled => "Cancelled",
        _ => "Unknown",
    };
}
