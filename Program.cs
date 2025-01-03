using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT ���� ��
var jwtKey = "blueseekers_0703_my_little_ocean_story"; // ��� Ű (�ּ� 16�� �̻�)
var key = Encoding.UTF8.GetBytes(jwtKey);

// ��Ʈ�ѷ� ���� �� �۷ι� ��� �����Ƚ� �߰�
builder.Services.AddControllers(options => {
    // ��� ��Ʈ�ѷ� ��ο� "api/v1" �����Ƚ��� �߰�
    options.Conventions.Add(new GlobalRoutePrefix("api/v1"));
});

// OpenAPI(������) ������ �����ϱ� ���� Endpoints API Explorer �߰�
builder.Services.AddEndpointsApiExplorer();

// ����� �ɼ� ����
builder.Services.Configure<RouteOptions>(options => {
    options.LowercaseUrls = true; // URL�� �ҹ��ڷ� ����
    options.AppendTrailingSlash = false; // URL ���� ������ ����
});

// JWT ���� ���� ���
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, // ������ ����
            ValidateAudience = true, // ����� ����
            ValidateLifetime = true, // ��ū ��ȿ�Ⱓ ����
            ValidateIssuerSigningKey = true, // ���� ����
            ValidIssuer = "http://localhost:7122", // ������ (�������� ����)
            ValidAudience = "http://localhost:7122", // ����� (�������� ����)
            IssuerSigningKey = new SymmetricSecurityKey(key) // ���� Ű
        };
        options.Events = new JwtBearerEvents {
            OnChallenge = context => {
                // �⺻ ���� ����
                context.HandleResponse();

                // ����� ���� ����
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\": \"Token is invalid or missing\"}");
            }
        };
    });

// Swagger(OpenAPI) ���� ������ ���� ���� �߰�
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

// �α� ���񽺸� DI �����̳ʿ� Singleton���� ���
builder.Services.AddSingleton<LoggingService>();

// UDP ������ BackgroundService�� ���
builder.Services.AddHostedService<UdpServerService>();

// CORS ���� (�ʿ��)
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(builder => {
        builder.WithOrigins("http://localhost:3000") // Ŭ���̾�Ʈ �ּ� (��: Unity �Ǵ� �� Ŭ���̾�Ʈ)
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var app = builder.Build();

// ����� ���� ���� �ڵ鸵 �̵���� �߰�
app.UseMiddleware<ErrorHandlingMiddleware>();

// ���� ȯ�濡�� Swagger UI Ȱ��ȭ
if (app.Environment.IsDevelopment()) {
    app.UseSwagger(); // Swagger JSON ����
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyLIO API V1"); // Swagger JSON ��ο� �̸� ����
        c.RoutePrefix = ""; // Swagger UI�� ��Ʈ("/") ��ο� ǥ��
    });
}

// HTTPS�� �����̷����ϴ� �̵����
app.UseHttpsRedirection();

// CORS Ȱ��ȭ (�ʿ��)
app.UseCors();

// JWT ���� �̵���� Ȱ��ȭ
app.UseAuthentication();

// ���� �ο� �̵����
app.UseAuthorization();

// ��Ʈ�ѷ��� �׼� ���Ʈ�� ���ø����̼ǿ� ����
app.MapControllers();

app.Run(); // ���ø����̼� ����
