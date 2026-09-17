namespace Backend.Cors;

public static class CorsService
{
    private static readonly string[] AllowedOrigins =
    [
        "http://localhost:3001",
        "http://localhost:3002",
        "http://127.0.0.1:3001",
        "http://127.0.0.1:3002",
        "https://frontend-production-0683.up.railway.app",
        "https://frontendpatients-production.up.railway.app"
    ];

    public static IServiceCollection AddCorsService(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Confirm_Med_Rule", policy =>
            {
                policy
                    .WithOrigins(AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();

                // En desarrollo se acepta cualquier puerto de localhost/127.0.0.1,
                // asi el front puede levantarse en el puerto que sea sin tocar esta lista.
                if (environment.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(origin =>
                        Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                        (uri.IsLoopback || AllowedOrigins.Contains(origin)));
                }
            });
        });

        return services;
    }
}
