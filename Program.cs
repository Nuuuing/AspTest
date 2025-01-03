using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient; // MySQL Connector 사용
using MySqlConnector;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 환경 변수 및 설정 값
var jwtKey = builder.Configuration["JwtSettings:Secret"] ?? throw new ArgumentNullException("JWT Secret is not configured.");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("Connection string is not configured.");

// 서비스 등록
ConfigureServices(builder.Services, jwtKey, connectionString);

// 애플리케이션 빌드
var app = builder.Build();

// 미들웨어 구성
ConfigureMiddleware(app);

app.Run();

void ConfigureServices(IServiceCollection services, string jwtKey, string connectionString) {
    // 컨트롤러 및 글로벌 경로 프리픽스 설정
    services.AddControllers(options => {
        options.Conventions.Add(new GlobalRoutePrefix("api/v1"));
    });

    // Swagger(OpenAPI) 설정
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c => {
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

    // JWT 인증 설정
    var key = Encoding.UTF8.GetBytes(jwtKey);
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options => {
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                ValidAudience = builder.Configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
            options.Events = new JwtBearerEvents {
                OnChallenge = context => {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"error\": \"Token is invalid or missing\"}");
                }
            };
        });

    // MySQL용 DB 연결 설정
    services.AddScoped<IDbConnection>(sp => new MySqlConnection(connectionString));

    // 기타 서비스 등록
    services.AddSingleton<LoggingService>();
    services.AddHostedService<UdpServerService>();

    // CORS 설정
    services.AddCors(options => {
        options.AddDefaultPolicy(builder => {
            builder.WithOrigins("http://localhost:3000")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
    });

    // 라우팅 설정
    services.Configure<RouteOptions>(options => {
        options.LowercaseUrls = true;
        options.AppendTrailingSlash = false;
    });
}

void ConfigureMiddleware(WebApplication app) {
    // 사용자 정의 에러 핸들링 미들웨어 추가
    app.UseMiddleware<ErrorHandlingMiddleware>();

    // 개발 환경에서 Swagger 활성화
    if (app.Environment.IsDevelopment()) {
        app.UseSwagger();
        app.UseSwaggerUI(c => {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyLIO API V1");
            c.RoutePrefix = "";
        });
    }

    // HTTPS 리다이렉션
    app.UseHttpsRedirection();

    // CORS 활성화
    app.UseCors();

    // 인증 및 권한 부여
    app.UseAuthentication();
    app.UseAuthorization();

    // 컨트롤러 매핑
    app.MapControllers();
}
