global using EDDatabase;
global using EDUtils;
global using Microsoft.EntityFrameworkCore;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMQ.Artemis.Client.Extensions.Hosting;
using EDCApi;
using EDOverwatch_Web.Authentication;
using EDOverwatch_Web.WebSockets;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration.Json;

namespace EDOverwatch_Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
#if DEBUG
            if (builder.Configuration.Sources.FirstOrDefault(c => c is JsonConfigurationSource j && j.Path == "secrets.json") is JsonConfigurationSource secrets)
            {
                builder.Configuration.Sources.Remove(secrets);
            }
            builder.Configuration.AddUserSecrets<Program>();
#endif
            builder.Configuration.AddEnvironmentVariables();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 8;
                    options.User.AllowedUserNameCharacters += " ";
                })
                .AddEntityFrameworkStores<EdDbContext>();


            builder.Services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKeyAuthentication", null)
                .AddScheme<AuthenticationSchemeOptions, CommanderApiKeyAuthenticationHandler>("CommanderApiKeyAuthenticationHandler", null);

            // Add services to the container.
            builder.Services.AddControllers()
                    .AddNewtonsoftJson();

            // builder.Services.AddEndpointsApiExplorer();
            // builder.Services.AddSwaggerGen();

            List<string> origins = (builder.Configuration.GetValue<string>("HTTP:CorsOrigin") ?? string.Empty).Split(",").ToList();

            ActiveMQ.Artemis.Client.Endpoint activeMqEndpont = ActiveMQ.Artemis.Client.Endpoint.Create(
                builder.Configuration.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
                builder.Configuration.GetValue<int>("ActiveMQ:Port"),
                builder.Configuration.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
                builder.Configuration.GetValue<string>("ActiveMQ:Password") ?? string.Empty);

            builder.Services.AddActiveMq("DN", new[] { activeMqEndpont })
                .AddAnonymousProducer<ActiveMqMessageProducer>();
            builder.Services.AddActiveMqHostedService();
            builder.Services.AddDbContext<EdDbContext>();
            builder.Services.AddSingleton<WebSocketServer>();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    corsBuilder =>
                    {
                        corsBuilder.WithOrigins(origins.ToArray())
                                .AllowCredentials()
                                .WithMethods(HttpMethods.Post, HttpMethods.Get, HttpMethods.Options)
                                .WithHeaders("content-type", "content-length");
                    });
                options.AddPolicy(name: "ApiCORS",
                                  policy =>
                                  {
                                      policy.AllowAnyOrigin()
                                          .WithMethods(HttpMethods.Get, HttpMethods.Options)
                                          .WithHeaders("content-type");
                                  });
            });
            builder.Services.AddScoped<FDevOAuth>();

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();

                // app.UseSwagger();
                // app.UseSwaggerUI();
            }

            WebSocketOptions webSocketOptions = new()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
            };
            foreach (string origin in origins)
            {
                webSocketOptions.AllowedOrigins.Add(origin);
            }

            app.UseWebSockets(webSocketOptions);
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.MapControllers();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Content-Security-Policy", "default-src 'self' https://darksession.github.io; style-src 'self' 'unsafe-inline'; frame-src https://darksession.github.io;");
                // trusted-types angular angular#bundler; require-trusted-types-for 'script';
                context.Response.Headers.Add("X-Frame-Options", "deny");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Add("Referrer-Policy", "strict-origin");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                await next();
            });

            app.MapFallbackToFile("index.html");

            await app.RunAsync();
        }
    }
}