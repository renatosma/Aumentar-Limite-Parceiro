using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.Configure<CompanyLayer>(config.GetSection("ServiceLayer"));
        services.Configure<AjusteSettings>(config.GetSection("Ajuste"));
        services.AddSingleton<ServiceLayerConnectionFactory>();
        services.AddSingleton<BusinessPartnerCreditoQueryService>();
        services.AddSingleton<AumentoCreditLineService>();
        services.AddTransient<FormPrincipal>();

        using var provider = services.BuildServiceProvider();
        var form = provider.GetRequiredService<FormPrincipal>();

        Application.Run(form);
    }
}
