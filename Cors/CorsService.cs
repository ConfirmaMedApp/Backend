namespace Backend.Cors;

public static class CorsService
{
    public static IServiceCollection AddCorsService(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Confirm_Med_Rule", policy =>
            {
                policy
                    .WithOrigins("http://localhost:3001", "http://localhost:3002", "https://confirmamedtest.netlify.app", "https://confirmamedtest.netlify.app/", "https://patientconfirmamedtest.netlify.app/", "https://patientconfirmamedtest.netlify.app", "https://frontend-production-0683.up.railway.app", "frontendpatients-production.up.railway.app")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        
        return services;
    }
}