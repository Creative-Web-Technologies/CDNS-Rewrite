using Cdns.Application.Feature.Container;
using Cdns.Application.Feature.Cusdec;
using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Cusdec;

/// <summary>
/// Container page backend (Member + Master). Gate order for every data endpoint:
///   1. Full subscription gate via <see cref="CusDecSubscriptionScope"/>.
///   2. AcknowledgementOnly check: OnlyForAcknowledgement subscriptions cannot access
///      container data (returns <see cref="SubscriptionAccessCode.AcknowledgementOnly"/>).
///   3. Data scope: <see cref="CusDecSubscriptionScope.ContainersScopedTo"/>.
/// </summary>
public sealed class ContainerService : IContainerService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly CdnsDbContext _db;
    private readonly IBusinessClock _clock;
    private readonly CusDecSubscriptionScope _scope;

    public ContainerService(
        CdnsDbContext db, IBusinessClock clock, CusDecSubscriptionScope scope)
    {
        _db = db;
        _clock = clock;
        _scope = scope;
    }

    // ── Endpoint 1: list ─────────────────────────────────────────────────────

    public async Task<CusDecAccess<ContainerListResponse>> GetContainersAsync(
        long memberSubscriptionId, ContainerListQuery query, CancellationToken ct = default)
    {
        var access = await ResolveGatedAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecAccess<ContainerListResponse>.Fail(access.Code);
        var sub = access.Value!;

        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize <= 0 ? DefaultPageSize : query.PageSize, 1, MaxPageSize);

        var q = _scope.ContainersScopedTo(sub).AsQueryable();

        // ── cusDecId filter ────────────────────────────────────────────────
        long? echoedCusDecId = null;
        string? echoedCusDecNumber = null;

        if (query.CusDecId.HasValue)
        {
            var cid = query.CusDecId.Value;
            var cd = await _scope.ContainerVisibleCusDecsScopedTo(sub)
                .Where(c => c.CusDecId == cid)
                .Select(c => new { c.CusDecId, c.CusDecNumber })
                .FirstOrDefaultAsync(ct);
            if (cd is null)
                return CusDecAccess<ContainerListResponse>.Fail(SubscriptionAccessCode.NotFound);
            echoedCusDecId     = cd.CusDecId;
            echoedCusDecNumber = cd.CusDecNumber;
            q = q.Where(c => c.CusDecId == cid);
        }

        // ── text filters ───────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(c =>
                (c.Container    != null && c.Container.ToLower().Contains(s))    ||
                (c.Location     != null && c.Location.ToLower().Contains(s))     ||
                (c.CurrentStatus!= null && c.CurrentStatus.ToLower().Contains(s))||
                (c.CusDec       != null && c.CusDec.CusDecNumber != null &&
                 c.CusDec.CusDecNumber.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var loc = query.Location.Trim().ToLower();
            q = q.Where(c => c.Location != null && c.Location.ToLower().Contains(loc));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var st = query.Status.Trim().ToLower();
            q = q.Where(c => c.CurrentStatus != null && c.CurrentStatus.ToLower().Contains(st));
        }

        // ── count (SQL) ────────────────────────────────────────────────────
        var total = await q.CountAsync(ct);

        if ((long)(page - 1) * pageSize >= total)
        {
            var emptyFilter = echoedCusDecId.HasValue
                ? new ContainerListFilter(echoedCusDecId.Value, echoedCusDecNumber)
                : null;
            return CusDecAccess<ContainerListResponse>.Ok(
                new ContainerListResponse([], total, page, pageSize, emptyFilter));
        }

        // ── sort + page (SQL): ArrivalDate DESC nulls-last, then id DESC ───
        // OrderBy(c.ArrivalDate == null) puts false (0) before true (1) → non-null dates first.
        var pageRows = await q
            .OrderBy(c => c.ArrivalDate == null)
            .ThenByDescending(c => c.ArrivalDate)
            .ThenByDescending(c => c.CusDecContainerId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContainerListRow(
                c.CusDecContainerId,
                c.Container,
                c.CusDecId,
                c.CusDec != null ? c.CusDec.CusDecNumber : null,
                c.CusDec != null ? c.CusDec.CusDecDate   : null,
                c.ArrivalDate,
                c.Location,
                c.Vessel,
                c.Type,
                c.CurrentStatus,
                c.ContainerPassNumber))
            .ToListAsync(ct);

        var items = pageRows.Select(r => new ContainerListItemDto(
            r.ContainerId,
            r.Container,
            r.CusDecId,
            r.CusDecNumber,
            r.CusDecDate is null ? null : new DateTimeOffset(DateTime.SpecifyKind(r.CusDecDate.Value, DateTimeKind.Utc)),
            r.ArrivalDate is null ? null : new DateTimeOffset(DateTime.SpecifyKind(r.ArrivalDate.Value, DateTimeKind.Utc)),
            r.Location,
            r.Vessel,
            r.Type,
            r.PassNumber <= 0 ? null : r.PassNumber,
            r.CurrentStatus)).ToList();

        var filter = echoedCusDecId.HasValue
            ? new ContainerListFilter(echoedCusDecId.Value, echoedCusDecNumber)
            : null;
        return CusDecAccess<ContainerListResponse>.Ok(
            new ContainerListResponse(items, total, page, pageSize, filter));
    }

    // ── Endpoint 2: detail ────────────────────────────────────────────────────

    public async Task<CusDecAccess<ContainerDetailDto>> GetContainerDetailAsync(
        long memberSubscriptionId, long containerContainerId, CancellationToken ct = default)
    {
        var access = await ResolveGatedAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecAccess<ContainerDetailDto>.Fail(access.Code);
        var sub = access.Value!;

        var row = await _scope.ContainersScopedTo(sub)
            .Where(c => c.CusDecContainerId == containerContainerId)
            .Select(c => new ContainerDetailRow(
                c.CusDecContainerId,
                c.Container,
                c.CusDecId,
                c.CusDec != null ? c.CusDec.CusDecNumber : null,
                c.CusDec != null ? c.CusDec.CusDecDate   : null,
                c.ArrivalDate,
                c.Location,
                c.Vessel,
                c.Voyage,
                c.Size,
                c.Type,
                c.CurrentStatus,
                c.ContainerPassNumber,
                c.CusDec != null ? c.CusDec.CusDecType             : null,
                c.CusDec != null ? c.CusDec.Channel                : null,
                c.CusDec != null ? c.CusDec.Origin                 : null,
                c.CusDec != null ? c.CusDec.BlNumber               : null,
                c.CusDec != null ? c.CusDec.InvoiceValue           : null,
                c.CusDec != null ? c.CusDec.DeclarantCompanyName   : null,
                c.CusDec != null ? c.CusDec.DeclarantTinNumber     : null,
                c.CusDec != null ? c.CusDec.ConsigneeCompanyName   : null,
                c.CusDec != null ? c.CusDec.ConsigneeTinNumber     : null))
            .FirstOrDefaultAsync(ct);

        if (row is null)
            return CusDecAccess<ContainerDetailDto>.Fail(SubscriptionAccessCode.NotFound);

        return CusDecAccess<ContainerDetailDto>.Ok(new ContainerDetailDto(
            row.ContainerId,
            row.Container,
            row.CusDecId,
            row.CusDecNumber,
            row.CusDecDate is null ? null : new DateTimeOffset(DateTime.SpecifyKind(row.CusDecDate.Value, DateTimeKind.Utc)),
            row.ArrivalDate is null ? null : new DateTimeOffset(DateTime.SpecifyKind(row.ArrivalDate.Value, DateTimeKind.Utc)),
            row.Location,
            row.Vessel,
            row.Voyage,
            row.Size,
            row.Type,
            row.CurrentStatus,
            row.PassNumber <= 0 ? null : row.PassNumber,
            row.CusDecType,
            row.Channel,
            row.Origin,
            row.BlNumber,
            row.InvoiceValue,
            row.DeclarantName,
            row.DeclarantTin,
            row.ConsigneeName,
            row.ConsigneeTin));
    }

    // ── Endpoint 3: status history ────────────────────────────────────────────

    public async Task<CusDecAccess<ContainerHistoryDto>> GetContainerHistoryAsync(
        long memberSubscriptionId, long containerContainerId, CancellationToken ct = default)
    {
        var access = await ResolveGatedAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecAccess<ContainerHistoryDto>.Fail(access.Code);
        var sub = access.Value!;

        // Access check: container must be in scope. ContainersScopedTo joins with CusDec to
        // apply the company-side WHERE; the result is projected to AnyAsync so no rows are
        // materialised — only the SQL EXISTS/count matters.
        var containerExists = await _scope.ContainersScopedTo(sub)
            .AnyAsync(c => c.CusDecContainerId == containerContainerId, ct);

        if (!containerExists)
            return CusDecAccess<ContainerHistoryDto>.Fail(SubscriptionAccessCode.NotFound);

        var resolver = await ContainerStatusResolver.LoadAsync(_db, ct);

        // Load all status rows for ONLY this container (ContainerService.cs:201 filters by
        // CusDecContainerId == containerContainerId). Sort newest-first; hidden-row exclusion
        // happens in memory — a SQL WHERE on IsShow would treat hidden rows as "unmapped"
        // (LEFT JOIN miss) rather than excluding them, which is wrong.
        var allRows = await _db.CusDecContainerStatuses
            .Where(s => s.CusDecContainerId == containerContainerId)
            .OrderBy(s => s.StatusDateTime == null)
            .ThenByDescending(s => s.StatusDateTime)
            .ThenByDescending(s => s.CusDecContainerStatusId)
            .Select(s => new StatusHistoryRow(s.Status, s.Description, s.StatusDateTime))
            .ToListAsync(ct);

        var visible = new List<ContainerHistoryRowDto>(allRows.Count);
        string? currentStatus = null;
        DateTime? lastStatusDate = null;
        var total = 0;

        foreach (var r in allRows)
        {
            if (resolver.IsHidden(r.Status, r.Description)) continue;
            total++;
            var (cdnsStatus, cdnsDesc) = resolver.ToCdns(r.Status, r.Description);
            if (currentStatus is null) // first visible row = newest
            {
                currentStatus = cdnsStatus;
                lastStatusDate = r.StatusDateTime;
            }
            visible.Add(new ContainerHistoryRowDto(
                cdnsStatus,
                cdnsDesc,
                r.StatusDateTime is null ? null
                    : new DateTimeOffset(DateTime.SpecifyKind(r.StatusDateTime.Value, DateTimeKind.Utc))));
        }

        return CusDecAccess<ContainerHistoryDto>.Ok(new ContainerHistoryDto(
            visible,
            currentStatus,
            lastStatusDate is null ? null
                : new DateTimeOffset(DateTime.SpecifyKind(lastStatusDate.Value, DateTimeKind.Utc))));
    }

    // ── Shared gate: full check + AcknowledgementOnly block ──────────────────

    private async Task<CusDecAccess<MemberSubscription>> ResolveGatedAsync(
        long memberSubscriptionId, CancellationToken ct)
    {
        var result = await _scope.ResolveAsync(memberSubscriptionId, ct);
        if (!result.IsOk) return result;

        if (result.Value!.SubscriptionActionTypeEnum == (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement)
            return CusDecAccess<MemberSubscription>.Fail(SubscriptionAccessCode.AcknowledgementOnly);

        return result;
    }

    // ── Internal records ──────────────────────────────────────────────────────

    private sealed record ContainerListRow(
        long ContainerId,
        string? Container,
        long? CusDecId,
        string? CusDecNumber,
        DateTime? CusDecDate,
        DateTime? ArrivalDate,
        string? Location,
        string? Vessel,
        string? Type,
        string? CurrentStatus,
        int PassNumber = 0);

    private sealed record ContainerDetailRow(
        long ContainerId,
        string? Container,
        long? CusDecId,
        string? CusDecNumber,
        DateTime? CusDecDate,
        DateTime? ArrivalDate,
        string? Location,
        string? Vessel,
        string? Voyage,
        string? Size,
        string? Type,
        string? CurrentStatus,
        int PassNumber,
        string? CusDecType,
        string? Channel,
        string? Origin,
        string? BlNumber,
        decimal? InvoiceValue,
        string? DeclarantName,
        string? DeclarantTin,
        string? ConsigneeName,
        string? ConsigneeTin);

    private sealed record StatusHistoryRow(string? Status, string? Description, DateTime? StatusDateTime);
}
