using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase {
    private readonly string _jwtKey = "blueseekers_0703_my_little_ocean_story"; // JWT 비밀 키
    private readonly string _issuer = "http://localhost:7122"; // 발행자 (프로토콜 포함)
    private readonly string _audience = "http://localhost:7122"; // 대상자 (프로토콜 포함)

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request) {
        // 사용자 인증 로직 (예: 데이터베이스 검증)
        if (request.Username == "test" && request.Password == "password") {
            // Access Token 생성
            var accessToken = GenerateToken(request.Username, TimeSpan.FromMinutes(15)); // 유효기간 15분

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
            var newAccessToken = GenerateToken(username, TimeSpan.FromMinutes(15)); // 새로운 Access Token 발급

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
