using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;

namespace AppInsightsKubernetes {
    public class Startup {
        public Startup (IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {
            // services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //     .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));

            services.AddControllers ();
            services.AddSwaggerGen (c => {
                c.SwaggerDoc ("v1", new OpenApiInfo { Title = "AppInsightsKubernetes", Version = "v1" });
            });

            // Configure Application Insights

            var options = new ApplicationInsightsServiceOptions () {
                InstrumentationKey = Environment.GetEnvironmentVariable ("InstrumentationKey"),

                EnableAdaptiveSampling = false
            };
            services.AddApplicationInsightsTelemetry (options);


            services.AddApplicationInsightsKubernetesEnricher ();

            services.ConfigureTelemetryModule<EventCounterCollectionModule> (
                (module, o) => {
                    // This removes all default counters, if any.
                    // module.Counters.Clear();

                    // This adds a user defined counter "MyCounter" from EventSource named "MyEventSource"
                    // module.Counters.Add(new EventCounterCollectionRequest("MyEventSource", "MyCounter"));

                    // This adds the system counter "gen-0-size" from "System.Runtime"

                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "time-in-gc"));

                    // Gen 0 size is the size of the generated code for the current generation.
                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "gen-0-size"));
                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "gen-0-gc-count"));

                    // Gen 1 size is the size of the generated code for the previous generation.
                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "gen-1-size"));
                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "gen-1-gc-count"));

                    // Gen 2 size is the size of the generated code for the previous generation.
                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "gen-2-size"));
                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "gen-2-gc-count"));

                    // The GC Heap Fragmentation (available on .NET 5 and later versions)
                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "gc-fragmentation"));

                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "gc-heap-size"));

                    // Thread Pool
                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "threadpool-thread-count"));
                    module.Counters.Add (new EventCounterCollectionRequest ("System.Runtime", "threadpool-completed-items-count"));

                    // Microsoft-AspNetCore-Server-Kestrel
                    module.Counters.Add (new EventCounterCollectionRequest ("Microsoft-AspNetCore-Server-Kestrel", "requests-per-second"));
                    module.Counters.Add (new EventCounterCollectionRequest ("Microsoft-AspNetCore-Server-Kestrel", "connection-queue-length"));
                });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
                app.UseSwagger ();
                app.UseSwaggerUI (c => c.SwaggerEndpoint ("/swagger/v1/swagger.json", "AppInsightsKubernetes v1"));
            }

            // Add Application Insights to the request pipeline

            var configuration = app.ApplicationServices.GetService<TelemetryConfiguration> ();

            var builder = configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
            // For older versions of the Application Insights SDK, use the following line instead:
            // var builder = configuration.TelemetryProcessorChainBuilder;

            // Using fixed rate sampling
            double fixedSamplingPercentage = 10;
            builder.UseSampling (fixedSamplingPercentage);

            builder.Build ();

            

            app.UseHttpsRedirection ();

            app.UseRouting ();

            // app.UseAuthentication();
            app.UseAuthorization ();

            app.UseEndpoints (endpoints => {
                endpoints.MapControllers ();
            });
        }
    }
}