using Cdns.Application.Feature.Cusdec;
using Cdns.Application.Feature.Dashboard;
using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.CusdecTables;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Cusdec;

/// <summary>
/// Shared CusDec page backend (Member + Master). Both roles see ONLY their own
/// subscriptions (MemberId == caller) — a master's self-held subscription has
/// MemberId == CompanyMasterMemberId. The page never exposes a subscription held
/// by a member under the master.
///
/// Tab rules are purely column-based:
///   New        = not claimed AND not disclaimed
///   In progress = claimed AND not disclaimed AND not completed
///   Completed  = claimed AND not disclaimed AND completed
///   Disclaimed = disclaimed
///
/// Tab filtering, counts, sorting and paging all happen in SQL via EF. Status
/// labels (and the IsExited hint) are computed in memory from the page rows'
/// status rows only — never from the whole table.
/// </summary>
public sealed class CusDecService : ICusDecService
{
    private const int DefaultPageSize = 4;

    private readonly CdnsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBusinessClock _clock;
    private readonly CusDecSubscriptionScope _scope;

    public CusDecService(
        CdnsDbContext db,
        ICurrentUser currentUser,
        IBusinessClock clock,
        CusDecSubscriptionScope scope)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _scope = scope;
    }

    // ── Endpoint 1: switcher ──────────────────────────────────────────────────

    public async Task<IReadOnlyList<CusDecSubscriptionDto>> GetUsableSubscriptionsAsync(
        CancellationToken ct = default)
    {
        var today = _clock.StartOfToday();
        var usable = await _scope.GetUsableOwnedSubscriptionsAsync(today, ct);

        return usable.Select(s => new CusDecSubscriptionDto(
            s.MemberSubscriptionId,
            TypeLabel(s.SubscriptionTypeEnum),
            s.Company.CompanyName,
            s.Company.TinNumber,
            s.SubscriptionName,
            s.ExpiryDate!.Value,
            ActionLabel(s.SubscriptionActionTypeEnum),
            IsAcknowledgementOnly(s.SubscriptionActionTypeEnum))).ToList();
    }

    // ── Endpoint 2: subscription strip + tab counts ──────────────────────────

    public async Task<CusDecAccess<CusDecSubscriptionStripDto>> GetSubscriptionStripAsync(
        long memberSubscriptionId, CancellationToken ct = default)
    {
        var access = await ResolveSubscriptionAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecAccess<CusDecSubscriptionStripDto>.Fail(access.Code);
        var sub = access.Value!;
        var isDeclarant = IsDeclarant(sub);

        var q = SideScoped(sub);
        int newCount, inProgressCount, completedCount, disclaimedCount;

        if (isDeclarant)
        {
            newCount        = await q.CountAsync(c => c.DeclarantClaimedById    == null && c.DeclarantDisclaimedById == null, ct);
            inProgressCount = await q.CountAsync(c => c.DeclarantClaimedById    != null && c.DeclarantDisclaimedById == null && c.DeclarantCompletedDateTime == null, ct);
            completedCount  = await q.CountAsync(c => c.DeclarantClaimedById    != null && c.DeclarantDisclaimedById == null && c.DeclarantCompletedDateTime != null, ct);
            disclaimedCount = await q.CountAsync(c => c.DeclarantDisclaimedById != null, ct);
        }
        else
        {
            newCount        = await q.CountAsync(c => c.ConsigneeClaimedById    == null && c.ConsigneeDisclaimedById == null, ct);
            inProgressCount = await q.CountAsync(c => c.ConsigneeClaimedById    != null && c.ConsigneeDisclaimedById == null && c.ConsigneeCompletedDateTime == null, ct);
            completedCount  = await q.CountAsync(c => c.ConsigneeClaimedById    != null && c.ConsigneeDisclaimedById == null && c.ConsigneeCompletedDateTime != null, ct);
            disclaimedCount = await q.CountAsync(c => c.ConsigneeDisclaimedById != null, ct);
        }

        var blocked = IsAcknowledgementOnly(sub.SubscriptionActionTypeEnum);
        return CusDecAccess<CusDecSubscriptionStripDto>.Ok(new CusDecSubscriptionStripDto(
            sub.MemberSubscriptionId,
            TypeLabel(sub.SubscriptionTypeEnum),
            sub.Company.CompanyName,
            sub.Company.TinNumber,
            sub.SubscriptionName,
            sub.ExpiryDate!.Value,
            ActionLabel(sub.SubscriptionActionTypeEnum),
            blocked,
            blocked,
            new CusDecTabCounts(newCount, inProgressCount, completedCount, disclaimedCount)));
    }

    // ── Endpoint 3: list ──────────────────────────────────────────────────────

    public async Task<CusDecAccess<CusDecListResponse>> GetCusDecsAsync(
        long memberSubscriptionId, CusDecListQuery query, CancellationToken ct = default)
    {
        var access = await ResolveSubscriptionAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecAccess<CusDecListResponse>.Fail(access.Code);
        var sub = access.Value!;

        var isDeclarant = IsDeclarant(sub);
        var tab     = NormalizeTab(query.Tab);
        var state   = NormalizeState(query.State);
        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize <= 0 ? DefaultPageSize : query.PageSize, 1, 100);
        var oldest   = string.Equals(query.Sort, "oldest", StringComparison.OrdinalIgnoreCase);

        // ── 1. Build SQL query (tab + state + search + office + date range) ────
        var q = BuildTabQuery(SideScoped(sub), isDeclarant, tab, state);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(c =>
                (c.CusDecNumber != null && c.CusDecNumber.ToLower().Contains(s)) ||
                (c.DeclarantCompanyName != null && c.DeclarantCompanyName.ToLower().Contains(s)) ||
                (c.ConsigneeCompanyName != null && c.ConsigneeCompanyName.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(query.Office))
            q = q.Where(c => c.OfficeCode == query.Office.Trim());

        if (!string.IsNullOrWhiteSpace(query.DeclarantTin))
        {
            var tin = StripWhitespace(query.DeclarantTin.Trim()).ToLower();
            if (tin.Length > 0 && tin.Length <= 20)
                q = q.Where(c => c.DeclarantTinNumber != null && c.DeclarantTinNumber.ToLower().StartsWith(tin));
        }

        if (!string.IsNullOrWhiteSpace(query.ConsigneeTin))
        {
            var tin = StripWhitespace(query.ConsigneeTin.Trim()).ToLower();
            if (tin.Length > 0 && tin.Length <= 20)
                q = q.Where(c => c.ConsigneeTinNumber != null && c.ConsigneeTinNumber.ToLower().StartsWith(tin));
        }

        if (!string.IsNullOrWhiteSpace(query.CusDecNumber))
        {
            var num = query.CusDecNumber.Trim().ToLower();
            q = q.Where(c => c.CusDecNumber != null && c.CusDecNumber.ToLower().Contains(num));
        }

        q = ApplyDateRangeSql(q, tab, isDeclarant, query.From, query.To);

        // ── 2. Count (stays in SQL) ────────────────────────────────────────────
        var total = await q.CountAsync(ct);

        if ((long)(page - 1) * pageSize >= total)
        {
            var blocked2 = IsAcknowledgementOnly(sub.SubscriptionActionTypeEnum);
            return CusDecAccess<CusDecListResponse>.Ok(
                new CusDecListResponse([], total, page, pageSize, blocked2));
        }

        // ── 3. Sort + page (SQL) ──────────────────────────────────────────────
        var paged = ApplySortSql(q, tab, isDeclarant, oldest)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var pageRows = await paged.Select(c => new PageRow(
            c.CusDecId,
            c.CusDecNumber,
            c.CusDecDate,
            c.RegistrationDate,
            c.OfficeCode,
            c.DeclarantCompanyName,
            c.ConsigneeCompanyName,
            isDeclarant ? c.DeclarantClaimedById    : c.ConsigneeClaimedById,
            isDeclarant ? c.DeclarantClaimedDateTime : c.ConsigneeClaimedDateTime,
            isDeclarant ? c.DeclarantDisclaimedById    : c.ConsigneeDisclaimedById,
            isDeclarant ? c.DeclarantDisclaimedDateTime : c.ConsigneeDisclaimedDateTime,
            isDeclarant ? c.DeclarantCompletedById    : c.ConsigneeCompletedById,
            isDeclarant ? c.DeclarantCompletedDateTime : c.ConsigneeCompletedDateTime
        )).ToListAsync(ct);

        if (pageRows.Count == 0)
        {
            var blocked2 = IsAcknowledgementOnly(sub.SubscriptionActionTypeEnum);
            return CusDecAccess<CusDecListResponse>.Ok(
                new CusDecListResponse([], total, page, pageSize, blocked2));
        }

        // ── 4. Batch-load statuses for the page rows only ─────────────────────
        var pageIds = pageRows.ConvertAll(r => r.CusDecId);

        // IsExited: one EXISTS query per status table, in-progress rows only.
        List<long> inProgressIds = tab == "tracking"
            ? pageRows.Where(r => r.ClaimedById != null).Select(r => r.CusDecId).ToList()
            : [];
        var exitedIds = await LoadExitedIdsAsync(inProgressIds, ct);

        var resolver = await AsycudaStatusResolver.LoadAsync(_db, ct);
        var statusMap = await LoadStatusMapAsync(pageIds, resolver.DefaultHiddenAsycudaCodes, ct);

        // ── 5. Build DTOs ──────────────────────────────────────────────────────
        var dtos = await BuildDtosAsync(pageRows, tab, statusMap, resolver, exitedIds, ct);

        var blocked = IsAcknowledgementOnly(sub.SubscriptionActionTypeEnum);
        return CusDecAccess<CusDecListResponse>.Ok(
            new CusDecListResponse(dtos, total, page, pageSize, blocked));
    }

    // ── Endpoint 4: acknowledge (legacy Claim) ────────────────────────────────

    public async Task<CusDecActionResponse> AcknowledgeAsync(
        long memberSubscriptionId, long cusDecId, CancellationToken ct = default)
    {
        var access = await ResolveSubscriptionAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecActionResponse.Denied(access.Code);
        var sub = access.Value!;
        var isDeclarant = IsDeclarant(sub);

        var cusDec = await _db.CusDecs.FirstOrDefaultAsync(c => c.CusDecId == cusDecId, ct);
        if (cusDec is null || !BelongsToSubscription(cusDec, sub, isDeclarant))
            return CusDecActionResponse.Of(CusDecActionStatus.CusDecNotFound);

        var disclaimedBy = isDeclarant ? cusDec.DeclarantDisclaimedById : cusDec.ConsigneeDisclaimedById;
        if (disclaimedBy is not null)
            return CusDecActionResponse.Of(CusDecActionStatus.AlreadyDisclaimed);

        var claimedBy = isDeclarant ? cusDec.DeclarantClaimedById : cusDec.ConsigneeClaimedById;
        if (claimedBy is null) // idempotent: only the first acknowledge writes
        {
            var now = _clock.Now().UtcDateTime;
            var me = _currentUser.MemberId;
            if (isDeclarant) { cusDec.DeclarantClaimedById = me; cusDec.DeclarantClaimedDateTime = now; }
            else             { cusDec.ConsigneeClaimedById = me; cusDec.ConsigneeClaimedDateTime = now; }
            await _db.SaveChangesAsync(ct);
        }

        return CusDecActionResponse.Of(CusDecActionStatus.Ok, await BuildSingleItemAsync(sub, cusDecId, ct));
    }

    // ── Endpoint 5: disclaim ──────────────────────────────────────────────────

    public async Task<CusDecActionResponse> DisclaimAsync(
        long memberSubscriptionId, long cusDecId, CancellationToken ct = default)
    {
        var access = await ResolveSubscriptionAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecActionResponse.Denied(access.Code);
        var sub = access.Value!;
        var isDeclarant = IsDeclarant(sub);

        var cusDec = await _db.CusDecs.FirstOrDefaultAsync(c => c.CusDecId == cusDecId, ct);
        if (cusDec is null || !BelongsToSubscription(cusDec, sub, isDeclarant))
            return CusDecActionResponse.Of(CusDecActionStatus.CusDecNotFound);

        var claimedBy   = isDeclarant ? cusDec.DeclarantClaimedById   : cusDec.ConsigneeClaimedById;
        var disclaimedBy = isDeclarant ? cusDec.DeclarantDisclaimedById : cusDec.ConsigneeDisclaimedById;

        // 409 if already claimed (can't disclaim after acknowledge)
        if (claimedBy is not null && disclaimedBy is null)
            return CusDecActionResponse.Of(CusDecActionStatus.AlreadyClaimed);

        // permanent: once disclaimed, never overwritten; re-disclaim is a no-op
        if (disclaimedBy is null)
        {
            var now = _clock.Now().UtcDateTime;
            var me = _currentUser.MemberId;
            if (isDeclarant) { cusDec.DeclarantDisclaimedById = me; cusDec.DeclarantDisclaimedDateTime = now; }
            else             { cusDec.ConsigneeDisclaimedById = me; cusDec.ConsigneeDisclaimedDateTime = now; }
            await _db.SaveChangesAsync(ct);
        }

        return CusDecActionResponse.Of(CusDecActionStatus.Ok, await BuildSingleItemAsync(sub, cusDecId, ct));
    }

    // ── Endpoint 6: complete ──────────────────────────────────────────────────

    public async Task<CusDecActionResponse> CompleteAsync(
        long memberSubscriptionId, long cusDecId, CancellationToken ct = default)
    {
        var access = await ResolveSubscriptionAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecActionResponse.Denied(access.Code);
        var sub = access.Value!;
        var isDeclarant = IsDeclarant(sub);

        var cusDec = await _db.CusDecs.FirstOrDefaultAsync(c => c.CusDecId == cusDecId, ct);
        if (cusDec is null || !BelongsToSubscription(cusDec, sub, isDeclarant))
            return CusDecActionResponse.Of(CusDecActionStatus.CusDecNotFound);

        var claimedBy    = isDeclarant ? cusDec.DeclarantClaimedById    : cusDec.ConsigneeClaimedById;
        var disclaimedBy = isDeclarant ? cusDec.DeclarantDisclaimedById : cusDec.ConsigneeDisclaimedById;
        var completedAt  = isDeclarant ? cusDec.DeclarantCompletedDateTime : cusDec.ConsigneeCompletedDateTime;

        if (disclaimedBy is not null) return CusDecActionResponse.Of(CusDecActionStatus.AlreadyDisclaimed);
        if (claimedBy is null)        return CusDecActionResponse.Of(CusDecActionStatus.NotClaimed);

        if (completedAt is null) // idempotent
        {
            var now = _clock.Now().UtcDateTime;
            var me = _currentUser.MemberId;
            if (isDeclarant) { cusDec.DeclarantCompletedById = me; cusDec.DeclarantCompletedDateTime = now; }
            else             { cusDec.ConsigneeCompletedById = me; cusDec.ConsigneeCompletedDateTime = now; }
            await _db.SaveChangesAsync(ct);
        }

        return CusDecActionResponse.Of(CusDecActionStatus.Ok, await BuildSingleItemAsync(sub, cusDecId, ct));
    }

    // ── Endpoint 7: reopen ────────────────────────────────────────────────────

    public async Task<CusDecActionResponse> ReopenAsync(
        long memberSubscriptionId, long cusDecId, CancellationToken ct = default)
    {
        var access = await ResolveSubscriptionAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecActionResponse.Denied(access.Code);
        var sub = access.Value!;
        var isDeclarant = IsDeclarant(sub);

        var cusDec = await _db.CusDecs.FirstOrDefaultAsync(c => c.CusDecId == cusDecId, ct);
        if (cusDec is null || !BelongsToSubscription(cusDec, sub, isDeclarant))
            return CusDecActionResponse.Of(CusDecActionStatus.CusDecNotFound);

        var completedAt = isDeclarant ? cusDec.DeclarantCompletedDateTime : cusDec.ConsigneeCompletedDateTime;
        if (completedAt is not null) // no-op if not completed
        {
            if (isDeclarant) { cusDec.DeclarantCompletedById = null; cusDec.DeclarantCompletedDateTime = null; }
            else             { cusDec.ConsigneeCompletedById = null; cusDec.ConsigneeCompletedDateTime = null; }
            await _db.SaveChangesAsync(ct);
        }

        return CusDecActionResponse.Of(CusDecActionStatus.Ok, await BuildSingleItemAsync(sub, cusDecId, ct));
    }

    // ── Endpoint 9: detail ───────────────────────────────────────────────────

    private const int ItemCap = 1_000;
    private const int TimelineCap = 500;

    public async Task<CusDecAccess<CusDecDetailDto>> GetCusDecDetailAsync(
        long memberSubscriptionId, long cusDecId, CancellationToken ct = default)
    {
        var access = await ResolveSubscriptionAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecAccess<CusDecDetailDto>.Fail(access.Code);
        var sub = access.Value!;
        var isDeclarant = IsDeclarant(sub);

        var cusDec = await SideScoped(sub)
            .FirstOrDefaultAsync(c => c.CusDecId == cusDecId, ct);
        if (cusDec is null)
            return CusDecAccess<CusDecDetailDto>.Fail(SubscriptionAccessCode.NotFound);

        var claimedById  = isDeclarant ? cusDec.DeclarantClaimedById  : cusDec.ConsigneeClaimedById;
        var disclaimedById = isDeclarant ? cusDec.DeclarantDisclaimedById : cusDec.ConsigneeDisclaimedById;
        var completedAt  = isDeclarant ? cusDec.DeclarantCompletedDateTime : cusDec.ConsigneeCompletedDateTime;

        var pageRow = new PageRow(
            cusDec.CusDecId, cusDec.CusDecNumber, cusDec.CusDecDate, cusDec.RegistrationDate,
            cusDec.OfficeCode, cusDec.DeclarantCompanyName, cusDec.ConsigneeCompanyName,
            claimedById,
            isDeclarant ? cusDec.DeclarantClaimedDateTime   : cusDec.ConsigneeClaimedDateTime,
            disclaimedById,
            isDeclarant ? cusDec.DeclarantDisclaimedDateTime : cusDec.ConsigneeDisclaimedDateTime,
            isDeclarant ? cusDec.DeclarantCompletedById      : cusDec.ConsigneeCompletedById,
            completedAt);

        var tab   = DeriveTab(pageRow);
        string? state = tab == "tracking" ? (claimedById is null ? "new" : "inprogress") : null;

        var resolver  = await AsycudaStatusResolver.LoadAsync(_db, ct);
        var statusMap = await LoadStatusMapAsync([cusDecId], resolver.DefaultHiddenAsycudaCodes, ct);
        var (currentStatus, lastUpdate) = ComputeStatus(pageRow, statusMap, resolver);

        var exitedIds = tab == "tracking" && claimedById is not null
            ? await LoadExitedIdsAsync([cusDecId], ct)
            : new HashSet<long>();
        var isExited = exitedIds.Contains(cusDecId);

        // Items — cap at 1 000, flag if more exist
        var rawItems = await _db.CusDecItems
            .Where(x => x.CusDecId == cusDecId)
            .OrderBy(x => x.CusDecItemNumber)
            .Take(ItemCap + 1)
            .Select(x => new CusDecItemDto(x.CusDecItemId, x.CusDecItemNumber,
                x.HsCode, x.Description, x.Quantity, x.UnitPrice, x.TotalValue, x.BlNumber))
            .ToListAsync(ct);
        var itemsTruncated = rawItems.Count > ItemCap;
        var items = itemsTruncated
            ? (IReadOnlyList<CusDecItemDto>)rawItems.Take(ItemCap).ToList()
            : rawItems;

        // Taxes — all, grouped by item number
        var rawTaxes = await _db.CusDecTaxes
            .Where(x => x.CusDecId == cusDecId)
            .OrderBy(x => x.ItemNumber).ThenBy(x => x.TaxRank)
            .Select(x => new { x.ItemNumber, Dto = new CusDecTaxDto(x.CusDecTaxId, x.TaxCode, x.TaxAmount, x.TaxRank) })
            .ToListAsync(ct);
        var taxes = rawTaxes
            .GroupBy(x => x.ItemNumber)
            .Select(g => new CusDecTaxGroupDto(g.Key, g.Select(x => x.Dto).ToList()))
            .ToList();

        // Inspections
        var inspRows = await _db.CusDecInspections
            .Where(x => x.CusDecId == cusDecId)
            .OrderBy(x => x.InspectionInstanceId).ThenBy(x => x.KeyFrdNumber)
            .ToListAsync(ct);

        var secRows = await _db.CusDecInspectionSectionTabs
            .Where(x => x.CusDecId == cusDecId)
            .OrderBy(x => x.InsTabExternalSystemNumber).ThenBy(x => x.KeySecNumber)
            .ToListAsync(ct);

        var secsByInspId = secRows
            .GroupBy(x => x.InsTabExternalSystemNumber)
            .ToDictionary(g => g.Key, g => g.ToList());

        var inspections = inspRows.Select(i =>
        {
            var secs = secsByInspId.TryGetValue(i.InspectionInstanceId, out var s) ? s : [];
            return new CusDecInspectionDto(
                i.CusDecInspectionId, i.InspectionInstanceId,
                i.IssueCode, i.Description, i.Value, i.KeyFrdNumber,
                i.FrdNfr, i.FrdUni, i.FrdUbr,
                secs.Select(x => new CusDecInspectionSectionDto(
                    x.CusDecInspectionSectionTabId, x.KeySecNumber, x.SectionName, ToDto(x.SectionDate)))
                    .ToList());
        }).ToList();

        return CusDecAccess<CusDecDetailDto>.Ok(new CusDecDetailDto(
            cusDec.CusDecId,
            cusDec.CusDecNumber,
            ToDto(cusDec.CusDecDate),
            ToDto(cusDec.RegistrationDate),
            ToDto(cusDec.ReleaseDate),
            cusDec.OfficeCode,
            cusDec.Origin,
            cusDec.Destination,
            cusDec.CusDecType,
            cusDec.Channel,
            cusDec.Incoterm,
            cusDec.BlNumber,
            cusDec.InvoiceValue,
            cusDec.NoOfItems,
            cusDec.NoOfContainers,
            cusDec.Exporter,
            cusDec.SupplierCompanyName,
            cusDec.DeclarantCompanyName,
            cusDec.DeclarantTinNumber,
            cusDec.ConsigneeCompanyName,
            cusDec.ConsigneeTinNumber,
            tab,
            state,
            currentStatus,
            ToDto(lastUpdate),
            isExited,
            items,
            itemsTruncated,
            taxes,
            inspections));
    }

    // ── Endpoint 10: timeline ─────────────────────────────────────────────────

    public async Task<CusDecAccess<CusDecTimelineDto>> GetCusDecTimelineAsync(
        long memberSubscriptionId, long cusDecId, CancellationToken ct = default)
    {
        var access = await ResolveSubscriptionAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecAccess<CusDecTimelineDto>.Fail(access.Code);
        var sub = access.Value!;
        var isDeclarant = IsDeclarant(sub);

        var cusDec = await SideScoped(sub)
            .FirstOrDefaultAsync(c => c.CusDecId == cusDecId, ct);
        if (cusDec is null)
            return CusDecAccess<CusDecTimelineDto>.Fail(SubscriptionAccessCode.NotFound);

        var claimedById  = isDeclarant ? cusDec.DeclarantClaimedById  : cusDec.ConsigneeClaimedById;
        var disclaimedById = isDeclarant ? cusDec.DeclarantDisclaimedById : cusDec.ConsigneeDisclaimedById;

        // Timeline only when this side has claimed and not disclaimed.
        if (claimedById is null || disclaimedById is not null)
            return CusDecAccess<CusDecTimelineDto>.Fail(SubscriptionAccessCode.NotFound);

        var completedAt = isDeclarant ? cusDec.DeclarantCompletedDateTime : cusDec.ConsigneeCompletedDateTime;
        var state = completedAt is null ? "inprogress" : "completed";

        var resolver = await AsycudaStatusResolver.LoadAsync(_db, ct);
        var hiddenArr = resolver.DefaultHiddenAsycudaCodes.Count == 0
            ? Array.Empty<string>()
            : resolver.DefaultHiddenAsycudaCodes.ToArray();

        // Load all rows from both tables — no Take; SQL pre-filter removes default-hidden codes.
        var docRows = await _db.CusDecStatuses
            .Where(s => s.CusDecId == cusDecId)
            .Where(s => hiddenArr.Length == 0 || s.Status == null || !hiddenArr.Contains(s.Status))
            .Select(s => new TimelineRow(s.CusDecStatusId, s.Status, s.StatusDateTime, false))
            .ToListAsync(ct);

        var inspRows = await _db.CusDecInspectStatuses
            .Where(s => s.CusDecId == cusDecId)
            .Where(s => hiddenArr.Length == 0 || !hiddenArr.Contains(s.Status))
            .Select(s => new TimelineRow(s.CusDecInspectStatusId, s.Status, s.StatusDateTime, true))
            .ToListAsync(ct);

        // Merge, sort: date DESC nulls last, then id DESC.
        var office = cusDec.OfficeCode;
        var merged = docRows.Concat(inspRows)
            .OrderBy(r => r.At is null)
            .ThenByDescending(r => r.At)
            .ThenByDescending(r => r.Id)
            .ToList();

        // Apply IsHidden in memory per row; cap visible rows at TimelineCap.
        var visible = new List<CusDecTimelineRowDto>(Math.Min(merged.Count, TimelineCap));
        var total = 0;
        foreach (var r in merged)
        {
            if (resolver.IsHidden(r.Status, office)) continue;
            total++;
            if (total <= TimelineCap)
                visible.Add(new CusDecTimelineRowDto(
                    resolver.ToCdns(r.Status, office),
                    r.At is null ? null : new DateTimeOffset(DateTime.SpecifyKind(r.At.Value, DateTimeKind.Utc)),
                    r.IsInspect ? "inspection" : "document"));
        }

        // Current status uses the already-loaded rows (avoid a second DB query).
        var statusRows = merged.Select(r => new StatusRow(cusDecId, r.Status, r.At));
        var (currentStatus, lastUpdate) = ResolveCurrentStatus(statusRows, office, resolver);

        var exitedIds = state == "inprogress"
            ? await LoadExitedIdsAsync([cusDecId], ct)
            : new HashSet<long>();
        var isExited = exitedIds.Contains(cusDecId);

        return CusDecAccess<CusDecTimelineDto>.Ok(new CusDecTimelineDto(
            visible,
            total > TimelineCap,
            currentStatus,
            ToDto(lastUpdate),
            isExited,
            state));
    }

    // ── Endpoint 8: office codes ──────────────────────────────────────────────

    public async Task<CusDecAccess<string[]>> GetOfficeCodesAsync(
        long memberSubscriptionId, CancellationToken ct = default)
    {
        var access = await ResolveSubscriptionAsync(memberSubscriptionId, ct);
        if (!access.IsOk) return CusDecAccess<string[]>.Fail(access.Code);
        var sub = access.Value!;

        var codes = await SideScoped(sub)
            .Where(c => c.OfficeCode != null && c.OfficeCode != "")
            .Select(c => c.OfficeCode!)
            .Distinct()
            .OrderBy(code => code)
            .ToArrayAsync(ct);

        return CusDecAccess<string[]>.Ok(codes);
    }

    // ── Subscription resolution ───────────────────────────────────────────────

    private Task<CusDecAccess<MemberSubscription>> ResolveSubscriptionAsync(
        long memberSubscriptionId, CancellationToken ct) =>
        _scope.ResolveAsync(memberSubscriptionId, ct);

    // ── SQL query builders ────────────────────────────────────────────────────

    private IQueryable<CusDec> SideScoped(MemberSubscription sub) =>
        _scope.CusDecsScopedTo(sub);

    private static IQueryable<CusDec> BuildTabQuery(
        IQueryable<CusDec> q, bool isDeclarant, string tab, string state)
    {
        if (isDeclarant)
        {
            q = tab switch
            {
                "completed"  => q.Where(c => c.DeclarantClaimedById != null && c.DeclarantDisclaimedById == null && c.DeclarantCompletedDateTime != null),
                "disclaimed" => q.Where(c => c.DeclarantDisclaimedById != null),
                _            => q.Where(c => c.DeclarantDisclaimedById == null && c.DeclarantCompletedDateTime == null),
            };
            if (tab == "tracking")
            {
                if (state == "new")        q = q.Where(c => c.DeclarantClaimedById == null);
                else if (state == "inprogress") q = q.Where(c => c.DeclarantClaimedById != null);
            }
        }
        else
        {
            q = tab switch
            {
                "completed"  => q.Where(c => c.ConsigneeClaimedById != null && c.ConsigneeDisclaimedById == null && c.ConsigneeCompletedDateTime != null),
                "disclaimed" => q.Where(c => c.ConsigneeDisclaimedById != null),
                _            => q.Where(c => c.ConsigneeDisclaimedById == null && c.ConsigneeCompletedDateTime == null),
            };
            if (tab == "tracking")
            {
                if (state == "new")        q = q.Where(c => c.ConsigneeClaimedById == null);
                else if (state == "inprogress") q = q.Where(c => c.ConsigneeClaimedById != null);
            }
        }
        return q;
    }

    private static IQueryable<CusDec> ApplyDateRangeSql(
        IQueryable<CusDec> q, string tab, bool isDeclarant, DateOnly? from, DateOnly? to)
    {
        if (from is null && to is null) return q;
        DateTime? fromUtc = from is null ? null : ColomboStartUtc(from.Value);
        DateTime? toUtc   = to   is null ? null : ColomboStartUtc(to.Value.AddDays(1)); // exclusive

        if (tab == "completed")
        {
            if (isDeclarant)
            {
                if (fromUtc.HasValue) q = q.Where(c => c.DeclarantCompletedDateTime >= fromUtc);
                if (toUtc.HasValue)   q = q.Where(c => c.DeclarantCompletedDateTime < toUtc);
            }
            else
            {
                if (fromUtc.HasValue) q = q.Where(c => c.ConsigneeCompletedDateTime >= fromUtc);
                if (toUtc.HasValue)   q = q.Where(c => c.ConsigneeCompletedDateTime < toUtc);
            }
        }
        else if (tab == "disclaimed")
        {
            if (isDeclarant)
            {
                if (fromUtc.HasValue) q = q.Where(c => c.DeclarantDisclaimedDateTime >= fromUtc);
                if (toUtc.HasValue)   q = q.Where(c => c.DeclarantDisclaimedDateTime < toUtc);
            }
            else
            {
                if (fromUtc.HasValue) q = q.Where(c => c.ConsigneeDisclaimedDateTime >= fromUtc);
                if (toUtc.HasValue)   q = q.Where(c => c.ConsigneeDisclaimedDateTime < toUtc);
            }
        }
        else // tracking: coalesce cus_dec_date ?? registration_date
        {
            if (fromUtc.HasValue) q = q.Where(c => (c.CusDecDate ?? c.RegistrationDate) >= fromUtc);
            if (toUtc.HasValue)   q = q.Where(c => (c.CusDecDate ?? c.RegistrationDate) < toUtc);
        }
        return q;
    }

    private static IOrderedQueryable<CusDec> ApplySortSql(
        IQueryable<CusDec> q, string tab, bool isDeclarant, bool oldest)
    {
        return (tab, isDeclarant, oldest) switch
        {
            ("completed", true,  false) => q.OrderByDescending(c => c.DeclarantCompletedDateTime).ThenByDescending(c => c.CusDecId),
            ("completed", true,  true)  => q.OrderBy(c => c.DeclarantCompletedDateTime).ThenBy(c => c.CusDecId),
            ("completed", false, false) => q.OrderByDescending(c => c.ConsigneeCompletedDateTime).ThenByDescending(c => c.CusDecId),
            ("completed", false, true)  => q.OrderBy(c => c.ConsigneeCompletedDateTime).ThenBy(c => c.CusDecId),
            ("disclaimed", true,  false) => q.OrderByDescending(c => c.DeclarantDisclaimedDateTime).ThenByDescending(c => c.CusDecId),
            ("disclaimed", true,  true)  => q.OrderBy(c => c.DeclarantDisclaimedDateTime).ThenBy(c => c.CusDecId),
            ("disclaimed", false, false) => q.OrderByDescending(c => c.ConsigneeDisclaimedDateTime).ThenByDescending(c => c.CusDecId),
            ("disclaimed", false, true)  => q.OrderBy(c => c.ConsigneeDisclaimedDateTime).ThenBy(c => c.CusDecId),
            (_, _, false) => q.OrderByDescending(c => c.CusDecDate ?? c.RegistrationDate).ThenByDescending(c => c.CusDecId),
            _             => q.OrderBy(c => c.CusDecDate ?? c.RegistrationDate).ThenBy(c => c.CusDecId),
        };
    }

    // ── Status batch loading (page rows only) ─────────────────────────────────

    // Load at most this many rows per CusDec per table, ordered newest-first.
    // A single production declaration has been observed with 34 375 rows; without the
    // cap every request for a page containing that row loads the whole history.
    // Per-ID WHERE + ORDER BY + TAKE generates an O(TopK) index-seek on PostgreSQL
    // and works unchanged with the InMemory test provider.
    private const int StatusTopK = 20;

    // SQL-level hidden exclusion covers only default-hidden codes (IsShow=false, no OfficeCode).
    // Office-specific hidden codes are skipped in memory by ComputeStatus via resolver.IsHidden,
    // because the OfficeCode lives on the parent cus_dec row, not on the status row, making a
    // correlated SQL join impractical inside a per-ID bounded query.
    private async Task<Dictionary<long, List<StatusRow>>> LoadStatusMapAsync(
        List<long> cusDecIds, IReadOnlySet<string> defaultHiddenCodes, CancellationToken ct)
    {
        // Materialize to string[] so EF/Npgsql generates = ANY(@p) rather than
        // attempting to translate an IReadOnlySet<string> interface.
        var hiddenArr = defaultHiddenCodes.Count == 0
            ? []
            : defaultHiddenCodes.ToArray();

        var allRows = new List<StatusRow>(cusDecIds.Count * StatusTopK * 2);
        foreach (var id in cusDecIds)
        {
            var normalRows = await _db.CusDecStatuses
                .Where(s => s.CusDecId == id && s.StatusDateTime != null)
                .Where(s => s.Status == null || !hiddenArr.Contains(s.Status))
                .OrderByDescending(s => s.StatusDateTime)
                .Take(StatusTopK)
                .Select(s => new StatusRow(id, s.Status, s.StatusDateTime))
                .ToListAsync(ct);
            allRows.AddRange(normalRows);

            var inspectRows = await _db.CusDecInspectStatuses
                .Where(s => s.CusDecId == id && s.StatusDateTime != null)
                .Where(s => s.Status == null || !hiddenArr.Contains(s.Status))
                .OrderByDescending(s => s.StatusDateTime)
                .Take(StatusTopK)
                .Select(s => new StatusRow(id, s.Status, s.StatusDateTime))
                .ToListAsync(ct);
            allRows.AddRange(inspectRows);
        }

        return allRows
            .GroupBy(s => s.CusDecId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    // ── IsExited batch check (in-progress rows on current page only) ──────────

    // Raw ASYCUDA status strings that signal exit — compared against s.Status directly,
    // not against the CDNS-mapped label, and regardless of a row's IsShow flag.
    private static readonly string[] ExitRawStatuses =
        [DashboardCalculator.ExitedStatus, DashboardCalculator.CancelledStatus];

    private async Task<HashSet<long>> LoadExitedIdsAsync(
        List<long> inProgressIds, CancellationToken ct)
    {
        if (inProgressIds.Count == 0) return [];

        var fromNormal = await _db.CusDecStatuses
            .Where(s => s.CusDecId != null
                     && inProgressIds.Contains(s.CusDecId!.Value)
                     && s.Status != null
                     && ExitRawStatuses.Contains(s.Status))
            .Select(s => s.CusDecId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var fromInspect = await _db.CusDecInspectStatuses
            .Where(s => s.CusDecId != null
                     && inProgressIds.Contains(s.CusDecId!.Value)
                     && ExitRawStatuses.Contains(s.Status))
            .Select(s => s.CusDecId!.Value)
            .Distinct()
            .ToListAsync(ct);

        return [..fromNormal, ..fromInspect];
    }

    // Shared helper: pick the newest non-hidden row from a pre-merged list.
    // Used by both the list page (via ComputeStatus) and the detail/timeline endpoints.
    private static (string? CurrentStatus, DateTime? LastUpdate) ResolveCurrentStatus(
        IEnumerable<StatusRow> rows, string? officeCode, AsycudaStatusResolver resolver)
    {
        var latest = rows
            .OrderByDescending(s => s.At ?? DateTime.MinValue)
            .Where(s => !resolver.IsHidden(s.Status, officeCode))
            .Select(s => (CdnsStatus: resolver.ToCdns(s.Status, officeCode), s.At))
            .FirstOrDefault();
        return (latest.CdnsStatus, latest.At);
    }

    private static (string? CurrentStatus, DateTime? LastUpdate) ComputeStatus(
        PageRow row, Dictionary<long, List<StatusRow>> statusMap, AsycudaStatusResolver resolver)
    {
        if (!statusMap.TryGetValue(row.CusDecId, out var rows) || rows.Count == 0)
            return (null, null);
        return ResolveCurrentStatus(rows, row.Office, resolver);
    }

    // ── DTO assembly ──────────────────────────────────────────────────────────

    private async Task<CusDecListItemDto?> BuildSingleItemAsync(
        MemberSubscription sub, long cusDecId, CancellationToken ct)
    {
        var isDeclarant = IsDeclarant(sub);
        var row = await SideScoped(sub)
            .Where(c => c.CusDecId == cusDecId)
            .Select(c => new PageRow(
                c.CusDecId, c.CusDecNumber, c.CusDecDate, c.RegistrationDate, c.OfficeCode,
                c.DeclarantCompanyName, c.ConsigneeCompanyName,
                isDeclarant ? c.DeclarantClaimedById    : c.ConsigneeClaimedById,
                isDeclarant ? c.DeclarantClaimedDateTime : c.ConsigneeClaimedDateTime,
                isDeclarant ? c.DeclarantDisclaimedById    : c.ConsigneeDisclaimedById,
                isDeclarant ? c.DeclarantDisclaimedDateTime : c.ConsigneeDisclaimedDateTime,
                isDeclarant ? c.DeclarantCompletedById    : c.ConsigneeCompletedById,
                isDeclarant ? c.DeclarantCompletedDateTime : c.ConsigneeCompletedDateTime
            ))
            .FirstOrDefaultAsync(ct);

        if (row is null) return null;

        var resolver = await AsycudaStatusResolver.LoadAsync(_db, ct);
        var statusMap = await LoadStatusMapAsync([row.CusDecId], resolver.DefaultHiddenAsycudaCodes, ct);

        // Determine tab from column state (the row may have changed since the last list load)
        var tab = DeriveTab(row);
        HashSet<long> exitedIds = tab == "tracking" && row.ClaimedById != null
            ? await LoadExitedIdsAsync([row.CusDecId], ct)
            : [];
        var dtos = await BuildDtosAsync([row], tab, statusMap, resolver, exitedIds, ct);
        return dtos[0];
    }

    private async Task<List<CusDecListItemDto>> BuildDtosAsync(
        List<PageRow> rows, string tab,
        Dictionary<long, List<StatusRow>> statusMap,
        AsycudaStatusResolver resolver,
        HashSet<long> exitedIds,
        CancellationToken ct)
    {
        var memberIds = rows
            .SelectMany(r => new[] { r.ClaimedById, r.DisclaimedById, r.CompletedById })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var names = new Dictionary<long, string?>();
        if (memberIds.Count > 0)
        {
            var members = await _db.Members
                .Where(m => memberIds.Contains(m.MemberId))
                .Select(m => new { m.MemberId, m.FirstName, m.LastName })
                .ToListAsync(ct);
            foreach (var m in members)
                names[m.MemberId] = NameOf(m.FirstName, m.LastName);
        }

        return rows.Select(row =>
        {
            var (currentStatus, lastUpdate) = ComputeStatus(row, statusMap, resolver);

            var rowTab   = tab == "auto" ? DeriveTab(row) : tab;
            string? rowState = rowTab == "tracking"
                ? (row.ClaimedById is null ? "new" : "inprogress")
                : null;

            return new CusDecListItemDto(
                row.CusDecId,
                row.CusDecNumber,
                ToDto(row.CusDecDate ?? row.RegistrationDate),
                row.Office,
                row.DeclarantName,
                row.ConsigneeName,
                currentStatus,
                ToDto(lastUpdate),
                rowTab,
                rowState,
                ToDto(row.ClaimedAt),
                row.ClaimedById is { } cid ? names.GetValueOrDefault(cid) : null,
                ToDto(row.DisclaimedAt),
                row.DisclaimedById is { } did ? names.GetValueOrDefault(did) : null,
                ToDto(row.CompletedAt),
                row.CompletedById is { } pid ? names.GetValueOrDefault(pid) : null,
                rowTab == "tracking" && rowState == "inprogress" && exitedIds.Contains(row.CusDecId));
        }).ToList();
    }

    // ── Small helpers ─────────────────────────────────────────────────────────

    private static string DeriveTab(PageRow r)
    {
        if (r.DisclaimedById is not null) return "disclaimed";
        if (r.CompletedAt    is not null) return "completed";
        return "tracking";
    }

    private static bool BelongsToSubscription(CusDec c, MemberSubscription sub, bool isDeclarant)
    {
        var sideCompanyId = isDeclarant ? c.DeclarantCompanyId : c.ConsigneeCompanyId;
        return sideCompanyId == sub.CompanyId;
    }

    private static bool IsDeclarant(MemberSubscription sub) =>
        sub.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant;

    private static bool IsAcknowledgementOnly(int actionType) =>
        actionType == (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement;

    private static string TypeLabel(int t) =>
        t == (int)SubscriptionTypeEnum.Declarant ? "Declarant" : "Consignee";

    private static string ActionLabel(int a) =>
        IsAcknowledgementOnly(a) ? "AcknowledgementOnly" : "General";

    private static string NormalizeTab(string? tab) => tab?.Trim().ToLowerInvariant() switch
    {
        "completed"  => "completed",
        "disclaimed" => "disclaimed",
        _            => "tracking",
    };

    private static string NormalizeState(string? state) => state?.Trim().ToLowerInvariant() switch
    {
        "new"        => "new",
        "inprogress" => "inprogress",
        _            => "all",
    };

    // Removes all whitespace characters from a string (used for TIN normalisation).
    private static string StripWhitespace(string s) =>
        string.Concat(s.Where(c => !char.IsWhiteSpace(c)));

    private static string? NameOf(string? first, string? last)
    {
        var n = $"{first} {last}".Trim();
        return string.IsNullOrEmpty(n) ? null : n;
    }

    private static DateTimeOffset? ToDto(DateTime? value) =>
        value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));

    // Convert a Colombo DateOnly to UTC DateTime (start of that Colombo day).
    private static DateTime ColomboStartUtc(DateOnly d)
    {
        var local = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, SriLankaTime.Zone);
    }

    // ── Internal records ──────────────────────────────────────────────────────

    private sealed record StatusRow(long CusDecId, string? Status, DateTime? At);

    private sealed record TimelineRow(long Id, string? Status, DateTime? At, bool IsInspect);

    private sealed record PageRow(
        long CusDecId,
        string? CusDecNumber,
        DateTime? CusDecDate,
        DateTime? RegistrationDate,
        string? Office,
        string? DeclarantName,
        string? ConsigneeName,
        long? ClaimedById,
        DateTime? ClaimedAt,
        long? DisclaimedById,
        DateTime? DisclaimedAt,
        long? CompletedById,
        DateTime? CompletedAt);
}
