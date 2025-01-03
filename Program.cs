using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT 설정 값
var jwtKey = "blueseekers_0703_my_little_ocean_story"; // 비밀 키 (최소 16자 이상)
var key = Encoding.UTF8.GetBytes(jwtKey);

// 컨트롤러 설정 및 글로벌 경로 프리픽스 추가
builder.Services.AddControllers(options => {
    // 모든 컨트롤러 경로에 "api/v1" 프리픽스를 추가
    options.Conventions.Add(new GlobalRoutePrefix("api/v1"));
});

// OpenAPI(스웨거) 문서를 생성하기 위해 Endpoints API Explorer 추가
builder.Services.AddEndpointsApiExplorer();

// 라우팅 옵션 설정
builder.Services.Configure<RouteOptions>(options => {
    options.LowercaseUrls = true; // URL을 소문자로 강제
    options.AppendTrailingSlash = false; // URL 끝에 슬래시 제거
});

// JWT 인증 서비스 등록
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, // 발행자 검증
            ValidateAudience = true, // 대상자 검증
            ValidateLifetime = true, // 토큰 유효기간 검증
            ValidateIssuerSigningKey = true, // 서명 검증
            ValidIssuer = "http://localhost:7122", // 발행자 (프로토콜 포함)
            ValidAudience = "http://localhost:7122", // 대상자 (프로토콜 포함)
            IssuerSigningKey = new SymmetricSecurityKey(key) // 서명 키
        };
        options.Events = new JwtBearerEvents {
            OnChallenge = context => {
                // 기본 응답 무시
                context.HandleResponse();

                // 사용자 정의 응답
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\": \"Token is invalid or missing\"}");
            }
        };
    });

// Swagger(OpenAPI) 문서 생성을 위한 서비스 추가
builder.Services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your token in the text box below.\nExample: Bearer <AccessToken>"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 로깅 서비스를 DI 컨테이너에 Singleton으로 등록
builder.Services.AddSingleton<LoggingService>();

// UDP 서버를 BackgroundService로 등록
builder.Services.AddHostedService<UdpServerService>();

// CORS 설정 (필요시)
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(builder => {
        builder.WithOrigins("http://localhost:3000") // 클라이언트 주소 (예: Unity 또는 웹 클라이언트)
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var app = builder.Build();

// 사용자 정의 에러 핸들링 미들웨어 추가
app.UseMiddleware<ErrorHandlingMiddleware>();

// 개발 환경에서 Swagger UI 활성화
if (app.Environment.IsDevelopment()) {
    app.UseSwagger(); // Swagger JSON 생성
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyLIO API V1"); // Swagger JSON 경로와 이름 설정
        c.RoutePrefix = ""; // Swagger UI를 루트("/") 경로에 표시
    });
}

// HTTPS로 리다이렉션하는 미들웨어
app.UseHttpsRedirection();

// CORS 활성화 (필요시)
app.UseCors();

// JWT 인증 미들웨어 활성화
app.UseAuthentication();

// 권한 부여 미들웨어
app.UseAuthorization();

// 컨트롤러와 액션 라우트를 애플리케이션에 매핑
app.MapControllers();

app.Run(); // 애플리케이션 실행
