using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient; // MySQL Connector ���
using MySqlConnector;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ȯ�� ���� �� ���� ��
var jwtKey = builder.Configuration["JwtSettings:Secret"] ?? throw new ArgumentNullException("JWT Secret is not configured.");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("Connection string is not configured.");

// ���� ���
ConfigureServices(builder.Services, jwtKey, connectionString);

// ���ø����̼� ����
var app = builder.Build();

// �̵���� ����
ConfigureMiddleware(app);

app.Run();

void ConfigureServices(IServiceCollection services, string jwtKey, string connectionString) {
    // ��Ʈ�ѷ� �� �۷ι� ��� �����Ƚ� ����
    services.AddControllers(options => {
        options.Conventions.Add(new GlobalRoutePrefix("api/v1"));
    });

    // Swagger(OpenAPI) ����
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

    // JWT ���� ����
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

    // MySQL�� DB ���� ����
    services.AddScoped<IDbConnection>(sp => new MySqlConnection(connectionString));

    // ��Ÿ ���� ���
    services.AddSingleton<LoggingService>();
    services.AddHostedService<UdpServerService>();

    // CORS ����
    services.AddCors(options => {
        options.AddDefaultPolicy(builder => {
            builder.WithOrigins("http://localhost:3000")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
    });

    // ����� ����
    services.Configure<RouteOptions>(options => {
        options.LowercaseUrls = true;
        options.AppendTrailingSlash = false;
    });
}

void ConfigureMiddleware(WebApplication app) {
    // ����� ���� ���� �ڵ鸵 �̵���� �߰�
    app.UseMiddleware<ErrorHandlingMiddleware>();

    // ���� ȯ�濡�� Swagger Ȱ��ȭ
    if (app.Environment.IsDevelopment()) {
        app.UseSwagger();
        app.UseSwaggerUI(c => {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyLIO API V1");
            c.RoutePrefix = "";
        });
    }

    // HTTPS �����̷���
    app.UseHttpsRedirection();

    // CORS Ȱ��ȭ
    app.UseCors();

    // ���� �� ���� �ο�
    app.UseAuthentication();
    app.UseAuthorization();

    // ��Ʈ�ѷ� ����
    app.MapControllers();
}
