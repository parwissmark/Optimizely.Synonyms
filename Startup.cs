using EPiServer.Cms.Shell;
using EPiServer.Cms.Shell.UI;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.DependencyInjection;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Synonyms.Plugins.Synonyms.Services;

namespace Synonyms;

public class Startup(IWebHostEnvironment webHostingEnvironment)
{
    public void ConfigureServices(IServiceCollection services)
    {
        if (webHostingEnvironment.IsDevelopment())
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(webHostingEnvironment.ContentRootPath, "App_Data"));
            services.Configure<SchedulerOptions>(options => options.Enabled = false);
        }

        services
            .AddCmsAspNetIdentity<ApplicationUser>()
            .AddCms()
            .AddAdminUserRegistration()
            .AddEmbeddedLocalization<Startup>();

        services.AddControllersWithViews();
        
        // Enable https://{domain}/Util/Register
        if (webHostingEnvironment.IsDevelopment())
        {
            services.AddAdminUserRegistration(options => { options.Behavior = RegisterAdminUserBehaviors.Enabled; });
        }
        
        // Graph dependency options
        services.ConfigureContentApiOptions(o =>
        {
            o.IncludeInternalContentRoots = true;
            o.IncludeSiteHosts = true;
            // o.EnablePreviewFeatures = true; // optional
        });
        services.AddContentDeliveryApi();
        services.AddContentGraph();
        services.AddHttpClient();
        
        services.AddTransient<ISynonymsService, SynonymsService>();
        services.AddTransient<IGraphSynonymsApiService, GraphSynonymsApiService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapContent();
        });
    }
}
