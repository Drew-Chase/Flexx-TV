using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Flexx.Server
{
    public class Startup
    {
        #region Public Methods

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Add custom mime types
            FileExtensionContentTypeProvider provider = new();
            provider.Mappings[".m3u8"] = "application/x-mpegURL";
            provider.Mappings[".M3U8"] = "application/x-mpegURL";
            provider.Mappings[".ts"] = "video/MP2T";
            provider.Mappings[".TS"] = "video/MP2T";

            app.UseCors();

            app.UseForwardedHeaders();

            app.UseMvc();

            app.UseRouting();
            app.UseStaticFiles();
            app.UseDefaultFiles();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(action =>
            {
                action.EnableEndpointRouting = false;
            });
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });
        }

        #endregion Public Methods
    }
}