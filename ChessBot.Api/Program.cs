namespace ChessBot.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
        
        RegisterServices(builder.Services);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
            app.UseDeveloperExceptionPage();
        else
            app.UseHttpsRedirection();

        app.UseCors();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Register your services here
        // services.AddScoped<IMyService, MyService>();
    }
}