using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using IdentityServer4.EntityFramework.Mappers;

namespace UNIPObjetivo.Identity.Server
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

            const string connectionString = "Server=DESKTOP-OUQFKI8;Database=IdentityServer;Trusted_Connection=True;MultipleActiveResultSets=true";
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var optionsContextBuilder = new Action<DbContextOptionsBuilder>(options => options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));      

            services.AddDbContext<ApplicationDbContext>(builder => builder.UseSqlServer(connectionString, sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)));
            services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();

            // Configuração do Identity Server In-Memory
            services.AddIdentityServer()
                //.AddInMemoryClients(Config.GetClients())
                //.AddInMemoryIdentityResources(Config.GetIdentityResources())
                //.AddInMemoryApiResources(Config.GetApiResources())
                //.AddInMemoryApiScopes(Config.GetApiScopes())
                //.AddTestUsers(Config.GetUsers())
                .AddAspNetIdentity<IdentityUser>()
                .AddDeveloperSigningCredential()
                .AddConfigurationStore(options => options.ConfigureDbContext = optionsContextBuilder)
                .AddOperationalStore(options => options.ConfigureDbContext = optionsContextBuilder);

            
            // This caused my migration to not get run
            services.AddDbContext<ConfigurationDbContext>(options => options.UseSqlServer(connectionString));
            services.AddSingleton(sp => new ConfigurationStoreOptions { ConfigureDbContext = optionsContextBuilder });
            // The above two lines needed to be moved below these lines
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

            InitializeDbTestData(app);

            //Habilitando o uso do Identity Server no nosso projeto
            app.UseIdentityServer();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private static void InitializeDbTestData(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
                serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.Migrate();
                serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                if (!context.Clients.Any())
                {
                    foreach (var client in Config.GetClients())
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.GetIdentityResources())
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    foreach (var scope in Config.GetApiScopes())
                    {
                        context.ApiScopes.Add(scope.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in Config.GetApiResources())
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                if (!userManager.Users.Any())
                {
                    foreach (var testUser in Config.GetUsers())
                    {
                        var identityUser = new IdentityUser(testUser.Username)
                        {
                            Id = testUser.SubjectId
                        };

                        userManager.CreateAsync(identityUser, "Password123!").Wait();
                        userManager.AddClaimsAsync(identityUser, testUser.Claims.ToList()).Wait();
                    }
                }
            }
        }
    }
}