using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.MemberLoginTables;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Persistence.Seed;

public static class AuthSeedData
{
    public static async Task SeedAsync(CdnsDbContext db, IPasswordHasher hasher)
    {
        if (await db.Companies.AnyAsync(c => c.TinNumber == "100000001")) return; // idempotent

        var testHash = hasher.HashNewPassword("Test@12345");

        // TIN A — status 1 (master, verified) + status 4/5 testing + a real working login account
        var companyA = new Company { TinNumber = "100000001", CompanyName = "Test Importer A Pvt Ltd", CompanyStatusEnum = 1 };
        var masterA = new Member { FirstName = "Master", LastName = "A", Email = "master-a@test.cdns", MemberType = MemberType.CompanyMasterMember };
        db.AddRange(companyA, masterA);
        await db.SaveChangesAsync();

        db.CompanyMemberMappings.Add(new CompanyMemberMapping { CompanyId = companyA.CompanyId, MemberId = masterA.MemberId });
        db.MemberUsers.Add(new MemberUser
        {
            MemberId = masterA.MemberId, UserName = masterA.Email!,
            Password = testHash.Hash, PasswordSalt = testHash.Salt,
            CreatedDateTime = DateTimeOffset.UtcNow, ActivationCode = 123456,
            GlobalIdentity = Guid.NewGuid(), LastOtpGeneratedDateTime = DateTimeOffset.UtcNow,
            UserStatusType = UserStatusType.Active, MemberType = MemberType.CompanyMasterMember,
            RequiredEregVerification = false
        });

        var memberA = new Member { FirstName = "Existing", LastName = "Member", Email = "existingmember-a@test.cdns", MemberType = MemberType.Member };
        db.Add(memberA);
        await db.SaveChangesAsync();
        db.MemberUsers.Add(new MemberUser
        {
            MemberId = memberA.MemberId, UserName = memberA.Email!,
            Password = testHash.Hash, PasswordSalt = testHash.Salt,
            CreatedDateTime = DateTimeOffset.UtcNow, ActivationCode = 123456,
            GlobalIdentity = Guid.NewGuid(), LastOtpGeneratedDateTime = DateTimeOffset.UtcNow,
            UserStatusType = UserStatusType.Active, MemberType = MemberType.Member,
            RequiredEregVerification = false
        });
        // "newmember-a@test.cdns" deliberately has no MemberUser row -> status 5 when tested against TIN A

        // TIN B — status 2 (master, unverified, same email) + status 3 (different email)
        var companyB = new Company { TinNumber = "100000002", CompanyName = "Test Importer B Pvt Ltd", CompanyStatusEnum = 1 };
        var masterB = new Member { FirstName = "Master", LastName = "B", Email = "master-b@test.cdns", MemberType = MemberType.CompanyMasterMember };
        db.AddRange(companyB, masterB);
        await db.SaveChangesAsync();
        db.CompanyMemberMappings.Add(new CompanyMemberMapping { CompanyId = companyB.CompanyId, MemberId = masterB.MemberId });
        db.MemberUsers.Add(new MemberUser
        {
            MemberId = masterB.MemberId, UserName = masterB.Email!,
            Password = "unused-placeholder", PasswordSalt = "unused-placeholder",
            CreatedDateTime = DateTimeOffset.UtcNow, ActivationCode = 654321,
            GlobalIdentity = Guid.NewGuid(), LastOtpGeneratedDateTime = DateTimeOffset.UtcNow,
            UserStatusType = UserStatusType.NotActive, MemberType = MemberType.CompanyMasterMember,
            RequiredEregVerification = true
        });

        // TIN C ("100000003") deliberately gets no Company row -> status 6

        await db.SaveChangesAsync();
    }
}
