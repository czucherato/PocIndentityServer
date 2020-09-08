using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace UNIPObjetivo.Identity.ApiClient
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
            IdentityModelEventSource.ShowPII = true;
            services.AddControllers();
            services.AddAuthentication(options => 
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false;
                        //options.SaveToken = true;
                        //options.Authority = "http://localhost:4000";
                        options.SetJwksOptions(new JwkOptions("http://localhost:4000/.well-known/openid-configuration/jwks"));
                        //options.SetJwksOptions(new JwkOptions("http://localhost:4000/jwks"));
                    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public static class JwksExtension
    {
        public static void SetJwksOptions(this JwtBearerOptions options, JwkOptions jwkOptions)
        {
            var httpClient = new HttpClient(options.BackchannelHttpHandler ?? new HttpClientHandler())
            {
                Timeout = options.BackchannelTimeout,
                MaxResponseContentBufferSize = 1024 * 1024 * 10 // 10 MB 
            };

            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                jwkOptions.JwksUri.OriginalString,
                new JwksRetriever(),
                new HttpDocumentRetriever(httpClient) { RequireHttps = options.RequireHttpsMetadata });
            options.TokenValidationParameters.ValidateAudience = false;
            options.TokenValidationParameters.ValidIssuer = jwkOptions.Issuer;
        }
    }

    public class JwkOptions
    {
        public JwkOptions(string jwksUri)
        {
            JwksUri = new Uri(jwksUri);
            Issuer = $"{JwksUri.Scheme}://{JwksUri.Authority}";
            KeepFor = TimeSpan.FromMinutes(15);
        }

        public JwkOptions(string jwksUri, TimeSpan cacheTime)
        {
            JwksUri = new Uri(jwksUri);
            Issuer = $"{JwksUri.Scheme}://{JwksUri.Authority}";
            KeepFor = cacheTime;
        }

        public JwkOptions(string jwksUri, string issuer, TimeSpan cacheTime)
        {
            JwksUri = new Uri(jwksUri);
            Issuer = issuer;
            KeepFor = cacheTime;
        }
        public string Issuer { get; private set; }

        public Uri JwksUri { get; private set; }

        public TimeSpan KeepFor { get; private set; }

    }

    public class JwksRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
    {
        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            return GetAsync(address, retriever, cancel);
        }

        /// <summary>
        /// Retrieves a populated <see cref="OpenIdConnectConfiguration"/> given an address and an <see cref="IDocumentRetriever"/>.
        /// </summary>
        /// <param name="address">address of the jwks uri.</param>
        /// <param name="retriever">the <see cref="IDocumentRetriever"/> to use to read the jwks</param>
        /// <param name="cancel"><see cref="CancellationToken"/>.</param>
        /// <returns>A populated <see cref="OpenIdConnectConfiguration"/> instance.</returns>
        public static async Task<OpenIdConnectConfiguration> GetAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw LogHelper.LogArgumentNullException(nameof(address));

            if (retriever == null)
                throw LogHelper.LogArgumentNullException(nameof(retriever));

            var doc = await retriever.GetDocumentAsync(address, cancel).ConfigureAwait(false);
            LogHelper.LogVerbose("IDX21811: Deserializing the string: '{0}' obtained from metadata endpoint into openIdConnectConfiguration object.", doc);
            var jwks = new JsonWebKeySet(doc);
            var openIdConnectConfiguration = new OpenIdConnectConfiguration()
            {
                JsonWebKeySet = jwks,
                JwksUri = address,
            };
            foreach (var securityKey in jwks.GetSigningKeys())
                openIdConnectConfiguration.SigningKeys.Add(securityKey);

            return openIdConnectConfiguration;
        }
    }
}