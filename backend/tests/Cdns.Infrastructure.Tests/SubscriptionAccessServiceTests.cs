using Cdns.Application.Feature.Subscriptions;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Services.Subscriptions;

namespace Cdns.Infrastructure.Tests;

public class SubscriptionAccessServiceTests
{
    private const long Owner = 1;

    // Builds an active company + an Activate subscription with a valid key, then
    // lets the caller mutate it before the check. Returns the tracked entity.
    private static async Task<MemberSubscription> SeedAsync(
        CdnsDbContext db,
        IMemberSubscriptionKeyService keys,
        DateTimeOffset expiry,
        long memberId = Owner,
        int companyStatus = (int)CompanyStatusEnum.Active,
        bool companyDeleted = false,
        long memberSubscriptionId = 1,
        int companyId = 1)
    {
        db.Companies.Add(new Company
        {
            CompanyId = companyId, CompanyName = "Acme", TinNumber = "123456789",
            IsDeleted = companyDeleted, CompanyStatusEnum = companyStatus,
        });
        var sub = new MemberSubscription
        {
            MemberSubscriptionId = memberSubscriptionId, MemberId = memberId, CompanyId = companyId,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = expiry, PaidAmount = 0m,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionName = "Plan", NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionRequestId = 0, SubscriptionId = 0,
        };
        db.MemberSubscriptions.Add(sub);
        await db.SaveChangesAsync();
        sub.Key = keys.Generate(sub);
        await db.SaveChangesAsync();
        return sub;
    }

    private static (CdnsDbContext db, FakeBusinessClock clock, IMemberSubscriptionKeyService keys, SubscriptionAccessService svc)
        NewHarness(string name)
    {
        var db = TestHelpers.CreateDb(name);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keys = FakeKeyServiceFactory.Create();
        return (db, clock, keys, TestHelpers.AccessService(db, clock, keys));
    }

    // ── CheckAsync: one per failure code + Ok ────────────────────────────────

    [Fact]
    public async Task CheckAsync_NotFound_WhenRowMissing()
    {
        var (db, _, _, svc) = NewHarness(nameof(CheckAsync_NotFound_WhenRowMissing));
        await using var _db = db;

        var result = await svc.CheckAsync(999, Owner);
        Assert.Equal(SubscriptionAccessCode.NotFound, result.Code);
        Assert.Null(result.Subscription);
    }

    [Fact]
    public async Task CheckAsync_NotOwner_WhenCallerDiffers()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_NotOwner_WhenCallerDiffers));
        await using var _db = db;
        await SeedAsync(db, keys, clock.StartOfToday().AddDays(30), memberId: Owner);

        var result = await svc.CheckAsync(1, callerMemberId: 999);
        Assert.Equal(SubscriptionAccessCode.NotOwner, result.Code);
    }

    [Fact]
    public async Task CheckAsync_Inactive_WhenDeactivated()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_Inactive_WhenDeactivated));
        await using var _db = db;
        var sub = await SeedAsync(db, keys, clock.StartOfToday().AddDays(30));
        sub.MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Deactivate;
        await db.SaveChangesAsync();

        var result = await svc.CheckAsync(1, Owner);
        Assert.Equal(SubscriptionAccessCode.Inactive, result.Code);
    }

    [Fact]
    public async Task CheckAsync_Expired_WhenExpiryYesterday()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_Expired_WhenExpiryYesterday));
        await using var _db = db;
        await SeedAsync(db, keys, clock.StartOfToday().AddDays(-1));

        var result = await svc.CheckAsync(1, Owner);
        Assert.Equal(SubscriptionAccessCode.Expired, result.Code);
    }

    [Fact]
    public async Task CheckAsync_Ok_WhenExpiryToday()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_Ok_WhenExpiryToday));
        await using var _db = db;
        await SeedAsync(db, keys, clock.StartOfToday());

        var result = await svc.CheckAsync(1, Owner);
        Assert.Equal(SubscriptionAccessCode.Ok, result.Code);
        Assert.NotNull(result.Subscription);
    }

    [Fact]
    public async Task CheckAsync_CompanyInactive_WhenCompanyNotActive()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_CompanyInactive_WhenCompanyNotActive));
        await using var _db = db;
        await SeedAsync(db, keys, clock.StartOfToday().AddDays(30),
            companyStatus: (int)CompanyStatusEnum.Suspended);

        var result = await svc.CheckAsync(1, Owner);
        Assert.Equal(SubscriptionAccessCode.CompanyInactive, result.Code);
    }

    [Fact]
    public async Task CheckAsync_CompanyInactive_WhenCompanyDeleted()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_CompanyInactive_WhenCompanyDeleted));
        await using var _db = db;
        await SeedAsync(db, keys, clock.StartOfToday().AddDays(30), companyDeleted: true);

        var result = await svc.CheckAsync(1, Owner);
        Assert.Equal(SubscriptionAccessCode.CompanyInactive, result.Code);
    }

    [Fact]
    public async Task CheckAsync_KeyMissing_WhenKeyNull()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_KeyMissing_WhenKeyNull));
        await using var _db = db;
        var sub = await SeedAsync(db, keys, clock.StartOfToday().AddDays(30));
        sub.Key = null;
        await db.SaveChangesAsync();

        var result = await svc.CheckAsync(1, Owner);
        Assert.Equal(SubscriptionAccessCode.KeyMissing, result.Code);
    }

    [Fact]
    public async Task CheckAsync_KeyMismatch_WhenTamperedExpiryDate()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_KeyMismatch_WhenTamperedExpiryDate));
        await using var _db = db;
        var sub = await SeedAsync(db, keys, clock.StartOfToday().AddDays(30));
        // Move expiry to a different but still-current date → passes Expired gate,
        // but the key no longer matches.
        sub.ExpiryDate = clock.StartOfToday().AddDays(31);
        await db.SaveChangesAsync();

        var result = await svc.CheckAsync(1, Owner);
        Assert.Equal(SubscriptionAccessCode.KeyMismatch, result.Code);
    }

    [Fact]
    public async Task CheckAsync_KeyMismatch_WhenTamperedActionType()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_KeyMismatch_WhenTamperedActionType));
        await using var _db = db;
        var sub = await SeedAsync(db, keys, clock.StartOfToday().AddDays(30));
        sub.SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement;
        await db.SaveChangesAsync();

        var result = await svc.CheckAsync(1, Owner);
        Assert.Equal(SubscriptionAccessCode.KeyMismatch, result.Code);
    }

    [Fact]
    public async Task CheckAsync_KeyMismatch_WhenTamperedPaidAmount()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_KeyMismatch_WhenTamperedPaidAmount));
        await using var _db = db;
        var sub = await SeedAsync(db, keys, clock.StartOfToday().AddDays(30));
        sub.PaidAmount = 1500m;
        await db.SaveChangesAsync();

        var result = await svc.CheckAsync(1, Owner);
        Assert.Equal(SubscriptionAccessCode.KeyMismatch, result.Code);
    }

    [Fact]
    public async Task CheckAsync_KeyMismatch_WhenInvalidBase64()
    {
        var (db, clock, keys, svc) = NewHarness(nameof(CheckAsync_KeyMismatch_WhenInvalidBase64));
        await using var _db = db;
        var sub = await SeedAsync(db, keys, clock.StartOfToday().AddDays(30));
        sub.Key = "!!!not-valid-base64!!!";
        await db.SaveChangesAsync();

        var result = await svc.CheckAsync(1, Owner);
        Assert.Equal(SubscriptionAccessCode.KeyMismatch, result.Code);
    }

    // ── GetUsableAsync (list helper) ─────────────────────────────────────────

    [Fact]
    public async Task GetUsableAsync_IncludesValid_ExcludesInactiveDeletedNullAndTamperedRows()
    {
        var (db, clock, keys, svc) = NewHarness(
            nameof(GetUsableAsync_IncludesValid_ExcludesInactiveDeletedNullAndTamperedRows));
        await using var _db = db;
        var today = clock.StartOfToday();
        var future = today.AddDays(30);

        // Companies: 1 active, 2 suspended, 3 deleted.
        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Active", TinNumber = "100000000", IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Active });
        db.Companies.Add(new Company { CompanyId = 2, CompanyName = "Suspended", TinNumber = "200000000", IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Suspended });
        db.Companies.Add(new Company { CompanyId = 3, CompanyName = "Deleted", TinNumber = "300000000", IsDeleted = true, CompanyStatusEnum = (int)CompanyStatusEnum.Active });

        MemberSubscription Row(long id, int companyId, DateTimeOffset expiry) => new()
        {
            MemberSubscriptionId = id, MemberId = Owner, CompanyId = companyId,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = expiry, PaidAmount = 0m,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionName = "Plan", NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionRequestId = 0, SubscriptionId = 0,
        };

        var valid = Row(1, 1, future);                 // included
        var expired = Row(2, 1, today.AddDays(-1));     // excluded: expiry
        var suspended = Row(3, 2, future);              // excluded: company suspended
        var deleted = Row(4, 3, future);                // excluded: company deleted
        var nullKey = Row(5, 1, future);                // excluded: null key
        var tampered = Row(6, 1, future);               // excluded: tampered key

        db.MemberSubscriptions.AddRange(valid, expired, suspended, deleted, nullKey, tampered);
        await db.SaveChangesAsync();

        // Assign valid keys to all, then break the two we want excluded by key.
        foreach (var s in new[] { valid, expired, suspended, deleted, tampered })
            s.Key = keys.Generate(s);
        nullKey.Key = null;
        await db.SaveChangesAsync();
        // Tamper after keying: change a hashed field so verification fails.
        tampered.PaidAmount = 999m;
        await db.SaveChangesAsync();

        var usable = await svc.GetUsableAsync(
            db.MemberSubscriptions.Where(s => s.MemberId == Owner), today);

        Assert.Single(usable);
        Assert.Equal(1, usable[0].MemberSubscriptionId);
        Assert.NotNull(usable[0].Company);
    }

    // ── Approve → CheckAsync end-to-end ──────────────────────────────────────

    [Fact]
    public async Task ApprovedSubscription_PassesCheckAsync_ForApprovedMember()
    {
        await using var db = TestHelpers.CreateDb(nameof(ApprovedSubscription_PassesCheckAsync_ForApprovedMember));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keys = FakeKeyServiceFactory.Create();

        // Active company 5, mapped to master 10.
        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Corp", TinNumber = "123456789", IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Active });
        db.CompanyMemberMappings.Add(new CompanyMemberMapping { CompanyId = 5, MemberId = 10, IsDeleted = false });
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1, MemberId = 1, CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false,
        });
        db.Subscriptions.Add(new Subscription
        {
            SubscriptionId = 1, SubscriptionName = "Basic Plan", Description = "ok",
            Amount = 500m, NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active, IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
        });
        await db.SaveChangesAsync();

        var approveSvc = new MasterSubscriptionRequestService(db, master, clock, keys);
        var result = await approveSvc.ApproveRequestAsync(1, 1);
        var success = Assert.IsType<ApproveRequestResult.Success>(result);

        var accessSvc = TestHelpers.AccessService(db, clock, keys);
        var check = await accessSvc.CheckAsync(success.MemberSubscriptionId, callerMemberId: 1);

        Assert.Equal(SubscriptionAccessCode.Ok, check.Code);
    }

    [Fact]
    public async Task ApprovedSubscription_PassesCheckAsync_AfterReloadFromFreshContext()
    {
        // NOTE: EF InMemory does NOT coerce decimal scale on round-trip (unlike
        // Postgres numeric(18,2)), so this test confirms the approve → reload →
        // verify flow but cannot itself reproduce the scale-coercion that broke the
        // key on real Postgres. The scale-independence of the hash is proven
        // deterministically in SubscriptionExpiryTests
        // (KeyService_VerifyEntity_IgnoresScaleDrift_ButNotValueChange); a true
        // Postgres round-trip of approve cannot run in this environment.
        const string dbName = nameof(ApprovedSubscription_PassesCheckAsync_AfterReloadFromFreshContext);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keys = FakeKeyServiceFactory.Create();
        long memberSubId;

        await using (var db = TestHelpers.CreateDb(dbName))
        {
            db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Corp", TinNumber = "123456789", IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Active });
            db.CompanyMemberMappings.Add(new CompanyMemberMapping { CompanyId = 5, MemberId = 10, IsDeleted = false });
            db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
            db.SubscriptionRequests.Add(new SubscriptionRequest
            {
                SubscriptionRequestId = 1, MemberId = 1, CompanyId = 5,
                SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
                SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
                RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false,
            });
            // Plan amount at numeric(18,4) scale.
            db.Subscriptions.Add(new Subscription
            {
                SubscriptionId = 1, SubscriptionName = "Basic Plan", Description = "ok",
                Amount = 25000.0000m, NoOfActiveDuration = 1,
                SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
                SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
                SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active, IsDeleted = false,
                SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            });
            await db.SaveChangesAsync();

            var approveSvc = new MasterSubscriptionRequestService(db, new FakeMasterUser(10), clock, keys);
            var success = Assert.IsType<ApproveRequestResult.Success>(await approveSvc.ApproveRequestAsync(1, 1));
            memberSubId = success.MemberSubscriptionId;
        }

        // Fresh context over the same in-memory store: the subscription is reloaded,
        // not the tracked instance from approve.
        await using (var db2 = TestHelpers.CreateDb(dbName))
        {
            var accessSvc = TestHelpers.AccessService(db2, clock, keys);
            var check = await accessSvc.CheckAsync(memberSubId, callerMemberId: 1);
            Assert.Equal(SubscriptionAccessCode.Ok, check.Code);
        }
    }
}
