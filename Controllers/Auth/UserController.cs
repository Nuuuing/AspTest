using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase {
    private readonly string _jwtKey = "blueseekers_0703_my_little_ocean_story"; // JWT 비밀 키
    private readonly string _issuer = "http://localhost:7122";
    private readonly string _audience = "http://localhost:7122";

    private readonly IUserService _userService;

    public UserController(IUserService userService) {
        _userService = userService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request) {
        if (request.Username == "test" && request.Password == "password") {
            // Access Token 생성
            var accessToken = GenerateToken(request.Username, TimeSpan.FromMinutes(30)); // 유효기간 30분

            // Refresh Token 생성
            var refreshToken = GenerateToken(request.Username, TimeSpan.FromDays(7)); // 유효기간 7일

            return Ok(new {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        return Unauthorized("Invalid username or password.");
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request) {
        try {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);

            // Refresh Token 검증
            var principal = tokenHandler.ValidateToken(request.RefreshToken, new TokenValidationParameters {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true, // 만료 여부 검증
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            }, out SecurityToken validatedToken);

            // Refresh Token이 유효하면 새로운 Access Token 생성
            var username = principal.Identity?.Name; // 사용자 이름 가져오기
            var newAccessToken = GenerateToken(username, TimeSpan.FromMinutes(30)); // 새로운 Access Token 발급 - 60분

            return Ok(new {
                AccessToken = newAccessToken
            });
        }
        catch (SecurityTokenExpiredException) {
            return Unauthorized("Refresh token has expired.");
        }
        catch (Exception ex) {
            return Unauthorized($"Invalid refresh token: {ex.Message}");
        }
    }

    private string GenerateToken(string username, TimeSpan validFor) {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            }),
            Expires = DateTime.UtcNow.Add(validFor), // 유효기간 설정
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [HttpPost("signup")]
    public IActionResult CreateUser([FromBody] UserCreateDto userCreateDto) {
        if (userCreateDto.userId.IsNullOrEmpty()) {
            return BadRequest("No information exists");
        }
        try {
            int createUser = _userService.CreateUser(userCreateDto);
            if (createUser > 0)
                return Ok(new { message = "User created successfully." });
            else
                return StatusCode(500, new { message = "Failed to create user." });
        }
        catch (Exception e) {
            return BadRequest(new { error = e.Message });
        }
    }

}

// 로그인 요청 DTO
public class LoginRequest {
    public string Username { get; set; }
    public string Password { get; set; }
}

// Refresh 요청 DTO
public class RefreshRequest {
    public string RefreshToken { get; set; }
}
