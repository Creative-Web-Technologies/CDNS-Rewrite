using Microsoft.EntityFrameworkCore;
using Cdns.Application.Features.Signup;
using Cdns.Infrastructure.Persistence;
using Cdns.Domain.Entities.MemberSignupTables; 
using Cdns.Domain.Entities.MemberLoginTables;
using Cdns.Domain.Enums; 
using System.Security.Cryptography; 
using Cdns.Infrastructure.Security;

namespace Cdns.Infrastructure.Services;

public class SignupService : ISignupService
{
    private readonly CdnsDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public SignupService(CdnsDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<VerifyTinResponse> VerifyTinAsync(string tin)
    {
        var status = await _dbContext.Database
            .SqlQueryRaw<int>("SELECT check_tin_signup_status({0}) AS \"Value\"", tin)
            .FirstOrDefaultAsync();

        var response = new VerifyTinResponse { StatusCode = status };

        switch (status)
        {
            case 1:
                response.Message = "TIN verified successfully.";
                break;
            case 2:
                response.Message = "This company is already registered. Please log in.";
                break;
            case 3:
                response.Message = "Invalid TIN. Must be at least 9 characters.";
                break;
            case 4:
                response.Message = "Company not found. Please check your TIN.";
                break;
            default:
                response.Message = "An unknown error occurred.";
                break;
        }

        return response;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // 1. Generate a random 6-digit OTP
        int otp = RandomNumberGenerator.GetInt32(100000, 999999);
        
        // 2. Generate a new GUID for this signup session
        string newGlobalIdentity = Guid.NewGuid().ToString();

        // 3. Create the Temporary Member record (No new columns used!)
        var tempMember = new TemporaryMember
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Mobile = request.Mobile,
            Nic = request.Nic,
            Designation = request.Designation,
            OneTimePassword = otp,
            CreatedDateTime = DateTime.UtcNow,
            GlobalIdentity = newGlobalIdentity // Use the GUID as the token
        };

        // 4. Save to Database
        _dbContext.TemporaryMembers.Add(tempMember);
        await _dbContext.SaveChangesAsync();

        // 5. Print the OTP to the terminal
        Console.WriteLine($"============== OTP FOR {request.Email} IS: {otp} ==============");

        // 6. Return Success with the GUID as the TempSignupToken
        return new RegisterResponse
        {
            Success = true,
            Message = "Registration successful. Please check your email for the OTP.",
            TempSignupToken = newGlobalIdentity.ToString()
        };
    }

    public async Task<bool> VerifyOtpAsync(VerifyOtpRequest request)
    {
        // 1. Parse the GUID token
        string globalId = request.TempSignupToken;
        if (string.IsNullOrEmpty(globalId))
        {
            Console.WriteLine($"FAILED: Token is invalid.");
            return false;
        }

        // 2. Find the TemporaryMember by GlobalIdentity
        var tempMember = await _dbContext.TemporaryMembers
            .FirstOrDefaultAsync(x => x.GlobalIdentity == globalId);

        if (tempMember == null)
        {
            Console.WriteLine($"FAILED: No TemporaryMember found with GlobalIdentity {globalId}.");
            return false;
        }

        if (tempMember.OneTimePassword != request.Otp)
        {
            Console.WriteLine($"FAILED: OTP mismatch. Expected {tempMember.OneTimePassword}, got {request.Otp}.");
            return false;
        }

        // 3. Mark as verified by setting OTP to 0 (since we can't use IsOtpVerified)
        tempMember.OneTimePassword = 0;
        await _dbContext.SaveChangesAsync();

        Console.WriteLine($"SUCCESS: OTP verified for {tempMember.Email}.");
        return true;
    }

    public async Task<bool> SetPasswordAsync(SetPasswordRequest request)
    {
        Console.WriteLine($"--- SetPasswordAsync called with token: '{request.TempSignupToken}' ---");
        
        string globalId = request.TempSignupToken;
        if (string.IsNullOrEmpty(globalId))
        {
            Console.WriteLine("FAILED: Token is invalid.");
            return false;
        }

        var tempMember = await _dbContext.TemporaryMembers
            .FirstOrDefaultAsync(x => x.GlobalIdentity == globalId);

        // 1. Verify that the member exists AND that OneTimePassword is 0 (meaning OTP was verified)
        if (tempMember == null || tempMember.OneTimePassword != 0)
        {
            Console.WriteLine("FAILED: Temp member not found or OTP not verified.");
            return false;
        }

        try
        {
            // 2. Hash the password
            var (hash, salt) = _passwordHasher.HashNewPassword(request.Password);

            // 3. Create the REAL Member
            var newMember = new Member
            {
                FirstName = tempMember.FirstName,
                LastName = tempMember.LastName,
                Email = tempMember.Email,
                Mobile = tempMember.Mobile,
                Nic = tempMember.Nic,
                Designation = tempMember.Designation,
                MemberType = MemberType.Member
            };

            _dbContext.Members.Add(newMember);
            await _dbContext.SaveChangesAsync(); 

            // 4. Create the REAL MemberUser
            var memberUser = new MemberUser
            {
                MemberId = newMember.MemberId, 
                Member = newMember, 
                UserName = tempMember.Email ?? string.Empty,
                Password = hash,
                PasswordSalt = salt,
                CreatedDateTime = DateTimeOffset.UtcNow,
                GlobalIdentity = Guid.NewGuid(),
                LastOtpGeneratedDateTime = DateTimeOffset.UtcNow,
                IsActiveOtp = true,
                MemberType = MemberType.Member,
                UserStatusType = UserStatusType.Active 
            };

            _dbContext.MemberUsers.Add(memberUser);
            
            // 5. Cleanup: Delete the TemporaryMember
            _dbContext.TemporaryMembers.Remove(tempMember);
            
            await _dbContext.SaveChangesAsync();
            
            Console.WriteLine($"SUCCESS: Member and MemberUser created for {tempMember.Email}.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DATABASE ERROR: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"INNER ERROR: {ex.InnerException.Message}");
            }
            return false;
        }
    }
}