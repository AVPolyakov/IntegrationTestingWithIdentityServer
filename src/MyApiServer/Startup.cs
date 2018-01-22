using System;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MyApiServer
{
    public class Startup
    {
        private readonly JwtBearerSettings bearerSettings;

        public Startup(IConfiguration configuration, JwtBearerSettings bearerSettings = null)
        {
            this.bearerSettings = bearerSettings;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services
            .AddAuthentication(GetAuthenticationOptions)
            .AddJwtBearer(GetJwtBearerOptions);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
            .UseAuthentication()
            .UseMvc();
        }

        private static void GetAuthenticationOptions(AuthenticationOptions options)
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }

        private void GetJwtBearerOptions(JwtBearerOptions options)
        {
            if (bearerSettings?.Action == null)
            {
                options.Authority = "http://localhost:5000";
                options.Audience = "http://localhost:5000/resources";
                options.RequireHttpsMetadata = false;
            }
            else
            {
                bearerSettings.Action(options);
            }
        }
    }

    public class JwtBearerSettings
    {
        public Action<JwtBearerOptions> Action { get; set; }
    }
}