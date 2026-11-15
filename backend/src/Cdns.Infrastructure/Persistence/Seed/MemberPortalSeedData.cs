using Cdns.Application.Feature.Dashboard;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.CusdecTables;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Persistence.Seed;

/// <summary>
/// Development-only seed for Member-portal data. Idempotent: it no-ops once a
/// Subscription plan exists, so it is safe to run on every startup. It must run
/// AFTER <see cref="AuthSeedData"/> because it attaches to the members and
/// companies that seed creates (existingmember-a@test.cdns, Company A, Company B).
///
/// Column-kind note: the CusDec table maps its DateTime columns to
/// "timestamp without time zone" (Unspecified kind), while every other table here
/// uses "timestamp with time zone" (Utc kind). The two locals below keep that straight.
/// </summary>
public static class MemberPortalSeedData
{
    private const string MemberEmail = "existingmember-a@test.cdns";
    private const string CompanyATin = "100000001";
    private const string CompanyBTin = "100000002";

    public static async Task SeedAsync(CdnsDbContext db)
    {
        // Idempotency marker: nothing else seeds Subscription rows.
        if (await db.Subscriptions.AnyAsync()) return;

        // Everything hangs off the AuthSeedData rows. If those are missing (seed ran
        // out of order, or the DB was partially created), bail rather than guess.
        var member = await db.Members.FirstOrDefaultAsync(m => m.Email == MemberEmail);
        var companyA = await db.Companies.FirstOrDefaultAsync(c => c.TinNumber == CompanyATin);
        var companyB = await db.Companies.FirstOrDefaultAsync(c => c.TinNumber == CompanyBTin);
        if (member is null || companyA is null || companyB is null) return;

        var masterA = await db.CompanyMemberMappings
            .Where(m => m.CompanyId == companyA.CompanyId)
            .Select(m => m.MemberId).FirstOrDefaultAsync();
        var masterB = await db.CompanyMemberMappings
            .Where(m => m.CompanyId == companyB.CompanyId)
            .Select(m => m.MemberId).FirstOrDefaultAsync();

        var nowOffset = DateTimeOffset.UtcNow;
        var nowUtc = DateTime.UtcNow;                                      // timestamptz columns
        var nowLocal = DateTime.SpecifyKind(nowUtc, DateTimeKind.Unspecified); // CusDec timestamp columns

        // ---- 1. Subscription plans (Declarant + Consignee) ----------------------
        var planDeclarantAnnual = new Subscription
        {
            SubscriptionName = "Declarant Annual",
            Description = "Annual declarant (CHA) subscription.",
            Amount = 25000m,
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = 1, // months
            SubscriptionTypeEnum = (int)SubscriptionType.Declarant,
            SubscriptionStatusEnum = 1,
            CreatedDateTime = nowOffset,
        };
        var planConsigneeAnnual = new Subscription
        {
            SubscriptionName = "Consignee Annual",
            Description = "Annual consignee (importer) subscription.",
            Amount = 18000m,
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = 1,
            SubscriptionTypeEnum = (int)SubscriptionType.Consignee,
            SubscriptionStatusEnum = 1,
            CreatedDateTime = nowOffset,
        };
        var planDeclarantMonthly = new Subscription
        {
            SubscriptionName = "Declarant Monthly",
            Description = "Monthly declarant (CHA) subscription.",
            Amount = 3000m,
            NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = 1,
            SubscriptionTypeEnum = (int)SubscriptionType.Declarant,
            SubscriptionStatusEnum = 1,
            CreatedDateTime = nowOffset,
        };
        db.Subscriptions.AddRange(planDeclarantAnnual, planConsigneeAnnual, planDeclarantMonthly);
        await db.SaveChangesAsync();

        // ---- 2. Subscription requests (all four statuses) -----------------------
        // Three Approved requests back the three member subscriptions below; the
        // remaining three exercise Pending / Rejected / Cancelled.
        var reqActiveDeclarant = NewRequest(member.MemberId, companyA.CompanyId, SubscriptionType.Declarant,
            SubscriptionRequestStatusEnum.Approved, nowOffset.AddDays(-200), masterA, "Approved — active declarant plan.");
        var reqActiveConsignee = NewRequest(member.MemberId, companyB.CompanyId, SubscriptionType.Consignee,
            SubscriptionRequestStatusEnum.Approved, nowOffset.AddDays(-150), masterB, "Approved — active consignee plan.");
        var reqExpired = NewRequest(member.MemberId, companyA.CompanyId, SubscriptionType.Declarant,
            SubscriptionRequestStatusEnum.Approved, nowOffset.AddDays(-480), masterA, "Approved — now expired.");
        var reqPending = NewRequest(member.MemberId, companyB.CompanyId, SubscriptionType.Consignee,
            SubscriptionRequestStatusEnum.Pending, nowOffset.AddDays(-3), responderId: null, comment: "Awaiting master approval.");
        // A pending request on Company A from the member — visible to Master A
        // (its own requests are excluded from the master's pending count).
        var reqPendingA = NewRequest(member.MemberId, companyA.CompanyId, SubscriptionType.Consignee,
            SubscriptionRequestStatusEnum.Pending, nowOffset.AddDays(-2), responderId: null, comment: "Awaiting Master A approval.");
        var reqRejected = NewRequest(member.MemberId, companyA.CompanyId, SubscriptionType.Declarant,
            SubscriptionRequestStatusEnum.Rejected, nowOffset.AddDays(-20), masterA, "Rejected by master.");
        var reqCancelled = NewRequest(member.MemberId, companyB.CompanyId, SubscriptionType.Consignee,
            SubscriptionRequestStatusEnum.Cancelled, nowOffset.AddDays(-30), responderId: null, comment: "Cancelled by member.");

        db.SubscriptionRequests.AddRange(
            reqActiveDeclarant, reqActiveConsignee, reqExpired, reqPending, reqPendingA, reqRejected, reqCancelled);
        await db.SaveChangesAsync();

        // ---- 3. Member subscriptions (2 active, 1 expired) ----------------------
        var subActiveDeclarant = NewMemberSubscription(
            reqActiveDeclarant, planDeclarantAnnual, member.MemberId, companyA.CompanyId, masterA,
            SubscriptionType.Declarant, MemberSubscriptionStatusEnum.Activate,
            start: nowOffset.AddDays(-200), expiry: nowOffset.AddDays(165), number: "MS-A-0001");
        var subActiveConsignee = NewMemberSubscription(
            reqActiveConsignee, planConsigneeAnnual, member.MemberId, companyB.CompanyId, masterB,
            SubscriptionType.Consignee, MemberSubscriptionStatusEnum.Activate,
            start: nowOffset.AddDays(-150), expiry: nowOffset.AddDays(215), number: "MS-B-0001");
        var subExpired = NewMemberSubscription(
            reqExpired, planDeclarantAnnual, member.MemberId, companyA.CompanyId, masterA,
            SubscriptionType.Declarant, MemberSubscriptionStatusEnum.Expired,
            start: nowOffset.AddDays(-480), expiry: nowOffset.AddDays(-115), number: "MS-A-0000");

        db.MemberSubscriptions.AddRange(subActiveDeclarant, subActiveConsignee, subExpired);

        // ---- 4. Member notifications (6, two unseen) ----------------------------
        db.MemberNotifications.AddRange(
            NewNotification(member.MemberId, "Welcome to CDNS", "Your member portal is ready.",
                NotificationType.Success, nowOffset.AddDays(-30), seen: true, showOnHome: true),
            NewNotification(member.MemberId, "Subscription approved", "Your declarant subscription is active.",
                NotificationType.Success, nowOffset.AddDays(-20), seen: true, showOnHome: true),
            NewNotification(member.MemberId, "Subscription expiring soon", "A subscription expires within 30 days.",
                NotificationType.Warning, nowOffset.AddDays(-10), seen: true, showOnHome: true),
            NewNotification(member.MemberId, "Request rejected", "A subscription request was rejected.",
                NotificationType.Danger, nowOffset.AddDays(-5), seen: true, showOnHome: false),
            NewNotification(member.MemberId, "New CusDec available", "A declaration matching your TIN is ready to acknowledge.",
                NotificationType.Info, nowOffset.AddHours(-20), seen: false, showOnHome: true),
            NewNotification(member.MemberId, "Container update", "A container status changed on one of your declarations.",
                NotificationType.Info, nowOffset.AddHours(-2), seen: false, showOnHome: true));

        // ---- 5. CusDec status mappings (for the statuses used below) ------------
        var cusDecStatusCodes = new (string Asycuda, string Cdns)[]
        {
            ("200", "Registered"),
            ("300", "Assessed"),
            ("400", "Released"),
            ("500", "Under Query"),
            ("700", DashboardCalculator.ExitedStatus),  // the exit status the dashboard counts as "completed"
            ("900", DashboardCalculator.CancelledStatus),
        };
        foreach (var (asycuda, cdns) in cusDecStatusCodes)
        {
            db.CusDecStatusMappings.Add(new CusDecStatusMapping
            {
                AsycudaStatus = asycuda,
                AsycudaDescription = $"ASYCUDA status {asycuda}",
                CdnsStatus = cdns,
                CdnsDescription = cdns,
                IsShow = true,
            });
        }

        // ---- 6. CusDecs (12: unacknowledged / claimed / disclaimed) -------------
        // TIN match is on the first 9 characters, so the seeded 9-char TINs match
        // directly. Each state group of four covers both sides (declarant/consignee)
        // against both companies: [decl A, cons A, decl B, cons B].
        var states = new[] { "unacknowledged", "claimed", "disclaimed" };
        var containerStatusCodes = new (string Status, string Desc)[]
        {
            ("GTIN", "Gate In"),
            ("LOAD", "Loaded"),
            ("DISC", "Discharged"),
        };

        long externalSeq = 900_000_000;
        var seq = 0;
        foreach (var state in states)
        {
            for (var j = 0; j < 4; j++)
            {
                seq++;
                var declarantSide = j % 2 == 0;
                var company = j < 2 ? companyA : companyB;
                externalSeq++;

                var cusDec = new CusDec
                {
                    CusDecNumber = $"CUSDEC-{DateTime.UtcNow.Year}-{seq:D4}",
                    GlobalIdentity = Guid.NewGuid(),
                    CusDecExternalSystemNumber = externalSeq,
                    LastSyncDateTime = nowLocal,
                    CusDecDate = nowLocal.AddDays(-seq * 2),
                    RegistrationDate = nowLocal.AddDays(-seq * 2),
                    OfficeCode = "CBHQ1",
                    Origin = "CN",
                    Destination = "LK",
                    Channel = declarantSide ? "Green" : "Yellow",
                    Incoterm = "CIF",
                    CusDecType = "IM4",
                    InvoiceValue = 10000m + seq * 1000m,
                    BlNumber = $"BL{externalSeq}",
                    Exporter = "Overseas Supplier Co.",
                    SupplierCompanyName = "Overseas Supplier Co.",
                };

                // TIN / company linkage on the side whose TIN matches the company.
                if (declarantSide)
                {
                    cusDec.DeclarantTinNumber = company.TinNumber;
                    cusDec.DeclarantCompanyId = company.CompanyId;
                    cusDec.DeclarantCompanyName = company.CompanyName;
                    cusDec.ConsigneeTinNumber = "700000000"; // unrelated importer
                    cusDec.ConsigneeCompanyName = "Unrelated Importer Pvt Ltd";
                }
                else
                {
                    cusDec.ConsigneeTinNumber = company.TinNumber;
                    cusDec.ConsigneeCompanyId = company.CompanyId;
                    cusDec.ConsigneeCompanyName = company.CompanyName;
                    cusDec.DeclarantTinNumber = "800000000"; // unrelated broker
                    cusDec.DeclarantCompanyName = "Unrelated Clearing Agent";
                }

                // Acknowledgement state, tied to the matching side and the member.
                switch (state)
                {
                    case "claimed" when declarantSide:
                        cusDec.DeclarantClaimedById = member.MemberId;
                        cusDec.DeclarantClaimedDateTime = nowLocal.AddDays(-seq);
                        break;
                    case "claimed":
                        cusDec.ConsigneeClaimedById = member.MemberId;
                        cusDec.ConsigneeClaimedDateTime = nowLocal.AddDays(-seq);
                        break;
                    case "disclaimed" when declarantSide:
                        cusDec.DeclarantDisclaimedById = member.MemberId;
                        cusDec.DeclarantDisclaimedDateTime = nowLocal.AddDays(-seq);
                        break;
                    case "disclaimed":
                        cusDec.ConsigneeDisclaimedById = member.MemberId;
                        cusDec.ConsigneeDisclaimedDateTime = nowLocal.AddDays(-seq);
                        break;
                        // "unacknowledged": leave all claim/disclaim fields null.
                }

                // Items (2 per declaration).
                var items = new List<CusDecItem>
                {
                    new()
                    {
                        CusDec = cusDec,
                        CusDecGlobalIdentity = cusDec.GlobalIdentity,
                        GlobalIdentity = Guid.NewGuid(),
                        CusDecItemNumber = 1,
                        HsCode = "8471.30",
                        Description = "Portable computers",
                        Quantity = "10",
                        UnitPrice = 500m,
                        TotalValue = 5000m,
                        BlNumber = cusDec.BlNumber,
                        CusDecExternalSystemNumber = externalSeq,
                        LastSyncDate = nowUtc,
                    },
                    new()
                    {
                        CusDec = cusDec,
                        CusDecGlobalIdentity = cusDec.GlobalIdentity,
                        GlobalIdentity = Guid.NewGuid(),
                        CusDecItemNumber = 2,
                        HsCode = "8523.51",
                        Description = "Solid-state storage devices",
                        Quantity = "20",
                        UnitPrice = 250m,
                        TotalValue = 5000m,
                        BlNumber = cusDec.BlNumber,
                        CusDecExternalSystemNumber = externalSeq,
                        LastSyncDate = nowUtc,
                    },
                };
                cusDec.NoOfItems = items.Count;

                // Containers (1-2) with their own status history.
                var containerCount = (seq % 2) + 1;
                var containers = new List<CusDecContainer>();
                for (var c = 0; c < containerCount; c++)
                {
                    var container = new CusDecContainer
                    {
                        CusDec = cusDec,
                        CusDecGlobalIdentity = cusDec.GlobalIdentity,
                        GlobalIdentity = Guid.NewGuid(),
                        Container = $"CONT{externalSeq}{c}",
                        Size = c == 0 ? "20" : "40",
                        Type = "GP",
                        Location = "Port of Colombo",
                        CurrentStatus = containerStatusCodes[^1].Status,
                        Vessel = "MV Test Carrier",
                        Voyage = $"V{seq:D3}",
                        ArrivalDate = nowUtc.AddDays(-seq),
                        ContainerKey = c + 1,
                        ContainerPassNumber = 0,
                        CusDecExternalSystemNumber = externalSeq,
                        LastSyncDate = nowUtc,
                    };
                    containers.Add(container);

                    var step = 0;
                    foreach (var (status, desc) in containerStatusCodes)
                    {
                        step++;
                        db.CusDecContainerStatuses.Add(new CusDecContainerStatus
                        {
                            CusDecContainer = container,
                            ContainerNumber = container.Container,
                            Status = status,
                            Description = desc,
                            StatusDateTime = nowUtc.AddDays(-seq).AddHours(step),
                            ContainerStatusNumber = step,
                            CusDecExternalSystemNumber = externalSeq,
                            LastSyncDate = nowUtc,
                        });
                    }
                }
                cusDec.NoOfContainers = containers.Count;

                // CusDec status history (Registered -> Assessed, plus Released when claimed).
                var historyCodes = state == "claimed"
                    ? new[] { cusDecStatusCodes[0], cusDecStatusCodes[1], cusDecStatusCodes[2] }
                    : new[] { cusDecStatusCodes[0], cusDecStatusCodes[1] };
                var statusStep = 0;
                foreach (var (asycuda, cdns) in historyCodes)
                {
                    statusStep++;
                    db.CusDecStatuses.Add(new CusDecStatus
                    {
                        CusDec = cusDec,
                        CusDecGlobalIdentity = cusDec.GlobalIdentity,
                        Status = asycuda,
                        Description = cdns,
                        StatusDateTime = nowUtc.AddDays(-seq).AddHours(statusStep),
                        CusDecStatusNumber = statusStep,
                        CusDecExternalSystemNumber = externalSeq,
                        TableName = "cus_dec_statuses",
                        LastSyncDateTime = nowUtc,
                    });
                }

                db.CusDecs.Add(cusDec);
                db.CusDecItems.AddRange(items);
                db.CusDecContainers.AddRange(containers);
            }
        }

        await db.SaveChangesAsync();

        // ---- 7. Completed (exited) & cancelled declarations --------------------
        // These drive the "Performance annually", "Total clearances" and status
        // widgets. They are spread across months of the current year and each
        // carries a first (Registered) and final (Totally exited) status so the
        // first→exit clearance time is derivable. Declarant-A rows are visible to
        // both the member and Master A; consignee-B rows are visible to the member.
        var year = nowUtc.Year;
        long completedExternalSeq = 950_000_000;

        void AddCompleted(Company company, bool declarantSide, int month, int day, double clearanceHours,
            string finalAsycuda, string channel)
        {
            completedExternalSeq++;
            var exitUtc = DateTime.SpecifyKind(
                new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)), 10, 0, 0),
                DateTimeKind.Utc);
            // Clamp to "now" so nothing exits in the future.
            if (exitUtc > nowUtc) exitUtc = nowUtc.AddHours(-1);
            var registeredUtc = exitUtc.AddHours(-clearanceHours);
            var assessedUtc = exitUtc.AddHours(-clearanceHours / 2);

            var cusDec = new CusDec
            {
                CusDecNumber = $"CUSDEC-{year}-C{completedExternalSeq % 100000:D4}",
                GlobalIdentity = Guid.NewGuid(),
                CusDecExternalSystemNumber = completedExternalSeq,
                LastSyncDateTime = DateTime.SpecifyKind(registeredUtc, DateTimeKind.Unspecified),
                CusDecDate = DateTime.SpecifyKind(registeredUtc, DateTimeKind.Unspecified),
                RegistrationDate = DateTime.SpecifyKind(registeredUtc, DateTimeKind.Unspecified),
                ReleaseDate = DateTime.SpecifyKind(exitUtc, DateTimeKind.Unspecified),
                OfficeCode = "CBHQ1",
                Origin = "CN",
                Destination = "LK",
                Channel = channel,
                Incoterm = "CIF",
                CusDecType = "IM4",
                InvoiceValue = 20000m + completedExternalSeq % 1000 * 10m,
                BlNumber = $"BL{completedExternalSeq}",
                Exporter = "Overseas Supplier Co.",
                SupplierCompanyName = "Overseas Supplier Co.",
                NoOfItems = 0,
                NoOfContainers = 0,
            };
            if (declarantSide)
            {
                cusDec.DeclarantTinNumber = company.TinNumber;
                cusDec.DeclarantCompanyId = company.CompanyId;
                cusDec.DeclarantCompanyName = company.CompanyName;
                cusDec.ConsigneeTinNumber = "700000000";
                cusDec.ConsigneeCompanyName = "Unrelated Importer Pvt Ltd";
            }
            else
            {
                cusDec.ConsigneeTinNumber = company.TinNumber;
                cusDec.ConsigneeCompanyId = company.CompanyId;
                cusDec.ConsigneeCompanyName = company.CompanyName;
                cusDec.DeclarantTinNumber = "800000000";
                cusDec.DeclarantCompanyName = "Unrelated Clearing Agent";
            }

            var steps = new (string Asycuda, string Cdns, DateTime At)[]
            {
                ("200", "Registered", registeredUtc),
                ("300", "Assessed", assessedUtc),
                (finalAsycuda, finalAsycuda == "900" ? DashboardCalculator.CancelledStatus : DashboardCalculator.ExitedStatus, exitUtc),
            };
            var n = 0;
            foreach (var (asycuda, cdns, at) in steps)
            {
                n++;
                db.CusDecStatuses.Add(new CusDecStatus
                {
                    CusDec = cusDec,
                    CusDecGlobalIdentity = cusDec.GlobalIdentity,
                    Status = asycuda,
                    Description = cdns,
                    StatusDateTime = at,
                    CusDecStatusNumber = n,
                    CusDecExternalSystemNumber = completedExternalSeq,
                    TableName = "cus_dec_statuses",
                    LastSyncDateTime = nowUtc,
                });
            }
            db.CusDecs.Add(cusDec);
        }

        // Declarant-A exits across the year (visible to member + Master A).
        var declAExits = new (int Month, int Day, double Hours)[]
        {
            (2, 12, 30), (4, 3, 18), (6, 21, 54), (8, 9, 12), (9, 27, 72), (10, 15, 24), (11, 6, 40),
        };
        foreach (var (m, d, h) in declAExits)
            AddCompleted(companyA, declarantSide: true, m, d, h, "700", "Green");

        // Consignee-B exits across the year (visible to the member).
        var consBExits = new (int Month, int Day, double Hours)[]
        {
            (3, 8, 20), (5, 19, 44), (7, 2, 16), (10, 28, 36),
        };
        foreach (var (m, d, h) in consBExits)
            AddCompleted(companyB, declarantSide: false, m, d, h, "700", "Yellow");

        // One cancelled declarant-A declaration (recent).
        AddCompleted(companyA, declarantSide: true, nowUtc.Month, Math.Max(1, nowUtc.Day - 4), 10, "900", "Red");

        // ---- 8. Payment summaries by TIN (for the Master payment card) ---------
        db.CusDecPaymentSummaries.AddRange(
            new CusDecPaymentSummary
            {
                DeclarantTinNumber = CompanyATin, ConsigneeTinNumber = "700000000",
                TotalCif = 5_000_000m, TotalTaxPaid = 1_250_000m, InspectionRecoveries = 75_000m,
                LastSyncDateTime = nowUtc,
            },
            new CusDecPaymentSummary
            {
                // Distinct consignee TIN so the (declarant, consignee) unique index
                // holds; still matches Company A on the declarant first-9 key.
                DeclarantTinNumber = CompanyATin, ConsigneeTinNumber = "700000001",
                TotalCif = 2_500_000m, TotalTaxPaid = 600_000m, InspectionRecoveries = 30_000m,
                LastSyncDateTime = nowUtc,
            },
            new CusDecPaymentSummary
            {
                DeclarantTinNumber = "800000000", ConsigneeTinNumber = CompanyBTin,
                TotalCif = 3_200_000m, TotalTaxPaid = 800_000m, InspectionRecoveries = 40_000m,
                LastSyncDateTime = nowUtc,
            });

        await db.SaveChangesAsync();
    }

    private static SubscriptionRequest NewRequest(
        long memberId, int companyId, SubscriptionType type,
        SubscriptionRequestStatusEnum status, DateTimeOffset requestedAt,
        long? responderId, string? comment)
    {
        var approvedOrResponded = status is not SubscriptionRequestStatusEnum.Pending;
        return new SubscriptionRequest
        {
            MemberId = memberId,
            CompanyId = companyId,
            RequestedDateTime = requestedAt,
            RespondDateTime = approvedOrResponded ? requestedAt.AddDays(1) : null,
            ResponderId = responderId,
            Comment = comment,
            ResponderComment = status switch
            {
                SubscriptionRequestStatusEnum.Approved => "Approved.",
                SubscriptionRequestStatusEnum.Rejected => "Does not meet requirements.",
                _ => null,
            },
            SubscriptionRequestNumber = $"SR-{requestedAt:yyyyMMdd}-{memberId}-{(int)type}-{(int)status}",
            SubscriptionRequestStatusEnum = (int)status,
            SubscriptionTypeEnum = (int)type,
            GlobalIdentity = Guid.NewGuid(),
            IsDeleted = false,
        };
    }

    private static MemberSubscription NewMemberSubscription(
        SubscriptionRequest request, Subscription plan, long memberId, int companyId, long masterMemberId,
        SubscriptionType type, MemberSubscriptionStatusEnum status,
        DateTimeOffset start, DateTimeOffset expiry, string number)
    {
        return new MemberSubscription
        {
            SubscriptionRequest = request,
            Subscription = plan,
            MemberId = memberId,
            CompanyId = companyId,
            CompanyMasterMemberId = masterMemberId,
            StartDate = start,
            ExpiryDate = expiry,
            PaidAmount = plan.Amount,
            PaidDate = start,
            SubscriptionAmount = plan.Amount,
            NoOfActiveDuration = plan.NoOfActiveDuration,
            SubscriptionDurationTypeEnum = plan.SubscriptionDurationTypeEnum,
            SubscriptionName = plan.SubscriptionName,
            MemberSubscriptionNumber = number,
            TransactionReferenceNumber = $"TXN-{number}",
            MemberSubscriptionStatusEnum = (int)status,
            SubscriptionTypeEnum = (int)type,
            SubscriptionActionTypeEnum = 0,
            GlobalIdentity = Guid.NewGuid(),
        };
    }

    private static MemberNotification NewNotification(
        long memberId, string title, string message, NotificationType type,
        DateTimeOffset at, bool seen, bool showOnHome)
    {
        return new MemberNotification
        {
            MemberId = memberId,
            Title = title,
            Message = message,
            NotificationType = type,
            NotificationDateTime = at,
            IsSeen = seen,
            SeenDateTime = seen ? at.AddMinutes(5) : null,
            ShowOnHome = showOnHome,
            IsDeleted = false,
        };
    }
}
