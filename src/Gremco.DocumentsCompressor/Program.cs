using Gremco.DocumentsCompressor.DocuWare;
using Gremco.DocumentsCompressor.Pdf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Gremco.DocumentsCompressor;

internal class Program
{
	public static async Task<int> Main(string[] args) {
		
		try
		{
			using var host = CreateHostBuilder(args).Build();
			await host.StartAsync();

			var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

			var startup = host.Services.GetRequiredService<Startup>();
			await startup.ExecuteAsync();

			lifetime.StopApplication();
			await host.WaitForShutdownAsync();

			return 0;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return 1;
		}
		finally
		{
			Environment.Exit(0);
		}
	}


	public static IHostBuilder CreateHostBuilder(string[] args)
	{
		var builder = Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration(cfg => { 
				cfg.AddJsonFile("appsettings.json");
				cfg.AddUserSecrets<Program>();
			})
			.UseSerilog((context, services, configuration) => configuration
				.ReadFrom.Configuration(context.Configuration)
				.ReadFrom.Services(services)
				.Enrich.FromLogContext())
			.ConfigureServices((hostContext, services) => {
				services.Configure<DocuWareConfiguration>(hostContext.Configuration.GetSection(nameof(DocuWareConfiguration)));
				services.AddHttpClient();
				services.AddSingleton(x => x.GetRequiredService<IOptions<DocuWareConfiguration>>().Value);
				services.AddSingleton<IDocuWareService, DocuWareService>();
				services.AddSingleton<IPdfManipulation, PdfManipulation>();
				services.AddSingleton<Startup>();
			});

		return builder;
	}
}