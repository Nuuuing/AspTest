public static class UserModule {

    public static IServiceCollection AddUserModule(this IServiceCollection services, string connectionString) {
        // Repository 등록
        services.AddScoped<IUserRepository>(provider => new UserRepository(connectionString));

        // Service 등록
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}