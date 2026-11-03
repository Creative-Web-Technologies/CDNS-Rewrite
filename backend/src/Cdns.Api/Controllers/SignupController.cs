using Microsoft.AspNetCore.Mvc;
using Cdns.Application.Features.Signup;

namespace Cdns.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignupController : ControllerBase
{
    private readonly ISignupService _signupService;

    public SignupController(ISignupService signupService)
    {
        _signupService = signupService;
    }

    [HttpPost("verify-tin")]
    public async Task<IActionResult> VerifyTin([FromBody] VerifyTinRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Tin))
            return BadRequest(new { message = "TIN is required." });

        var result = await _signupService.VerifyTinAsync(request.Tin);

        if (result.StatusCode != 1)
            return BadRequest(result); 

        return Ok(result); 
    }

    // THIS IS THE NEW ENDPOINT
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Tin) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "TIN and Email are required." });

        var result = await _signupService.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var result = await _signupService.VerifyOtpAsync(request);
    
        if (!result)
            return BadRequest(new { success = false, message = "Invalid or expired OTP. Check backend terminal." });

        return Ok(new { message = "OTP verified successfully.", success = true });
    }
    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var result = await _signupService.SetPasswordAsync(request);

        if (!result)
            return BadRequest(new { message = "Invalid session or OTP not verified." });

        // TODO: Generate Session Cookie here!

        return Ok(new { message = "Password set successfully. You are now logged in.", success = true });
    }
    
}