using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace HealthChecks
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddHealthChecks()
            .AddAsyncCheck("SqlServer", async () =>
            {
                using (var connection = new SqlConnection("Server=localhost;Database=master;User Id=sa;Password=P@ssw0rd!;"))
                {
                    try
                    {
                        await connection.OpenAsync();
                    }
                    catch (Exception)
                    {
                        return await Task.FromResult(HealthCheckResult.Unhealthy());
                    }
                    return await Task.FromResult(HealthCheckResult.Healthy());
                }
            })
            .AddAsyncCheck("Redis", async () =>
            {
                using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379"))
                {
                    try
                    {
                        var db = redis.GetDatabase(0);
                    }
                    catch (Exception)
                    {
                        return await Task.FromResult(HealthCheckResult.Unhealthy());
                    }
                }
                return await Task.FromResult(HealthCheckResult.Healthy());
            })
            .AddAsyncCheck("HealthCheck", async () =>
           {
               using (HttpClient client = new HttpClient())
               {
                   try
                   {
                       var response = await client.GetAsync("http://localhost:5000");
                       if (!response.IsSuccessStatusCode)
                       {
                           throw new Exception("Url not responding with 200 OK");
                       }
                   }
                   catch (Exception)
                   {
                       return await Task.FromResult(HealthCheckResult.Unhealthy());
                   }
               }
               return await Task.FromResult(HealthCheckResult.Healthy());
           });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            var options = new HealthCheckOptions();
            options.ResultStatusCodes[HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable;

            options.ResponseWriter = async (ctx, rpt) =>
            {
                var result = JsonConvert.SerializeObject(new
                {
                    status = rpt.Status.ToString(),
                    data = rpt.Entries.Select(e => new
                    {
                        key = e.Key,
                        value = Enum.GetName(typeof(HealthStatus), e.Value.Status),
                        code = (HealthStatus)e.Value.Status
                    })
                }, Formatting.None, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                ctx.Response.ContentType = MediaTypeNames.Application.Json;
                await ctx.Response.WriteAsync(result);
            };

            app.UseHealthChecks("/health-check", options);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
