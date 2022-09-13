using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.eShopOnContainers.Services.Identity.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // 此方法由运行时调用。 使用此方法向容器添加服务.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // 注册App监控(搁置)
            RegisterAppInsights(services);

            #region  微软 Identity 类库部分
            services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(Configuration["ConnectionString"],
            sqlServerOptionsAction: sqlOptions =>
            {
                // 迁移的应用
                sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                // 失败规则
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
            }));

            // 映射自定义的User，Role 添加用户角色配置
            services.AddIdentity<ApplicationUser, IdentityRole>()
                //配置使用EF持久化存储
                .AddEntityFrameworkStores<ApplicationDbContext>()
                //配置默认的TokenProvider用于变更密码和修改email时生成Token
                .AddDefaultTokenProviders();
            #endregion

            #region 配置数据及保护数据部分
            services.Configure<AppSettings>(Configuration);

            if (Configuration.GetValue<string>("IsClusterEnv") == bool.TrueString)
            {
                // 数据保护
                services.AddDataProtection(opts =>
                {
                    opts.ApplicationDiscriminator = "eshop.identity";
                })
                //将数据保护系统配置为将密钥持久保存到 Redis 数据库中的指定密钥
                //RedisKey:DataProtection-Keys
                .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(Configuration["DPConnectionString"]), "DataProtection-Keys");
            }

            #endregion

            #region 健康检查 服务部分
            // 健康检查 Microsoft.Extensions.Diagnostics.HealthChecks
            services.AddHealthChecks()
                    .AddCheck("self", () => HealthCheckResult.Healthy())
                    // HealthChecks.SqlServer包,检查数据库连接是否正常
                    .AddSqlServer(Configuration["ConnectionString"],
                        name: "IdentityDB-check",
                        tags: new string[] { "IdentityDB" });
            #endregion

            // 用户登录服务：Microsoft.Extensions.Identity.Core包
            services.AddTransient<ILoginService<ApplicationUser>, EFLoginService>();
            // 跳转服务
            services.AddTransient<IRedirectService, RedirectService>();

            #region Ids4 服务部分
            var connectionString = Configuration["ConnectionString"];
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            services.AddIdentityServer(x =>
            {
                x.IssuerUri = "null";
                //设置 cookie 生命周期（仅在使用 IdentityServer 提供的 cookie 处理程序时有效）
                x.Authentication.CookieLifetime = TimeSpan.FromHours(2);
            })
            // 本地开发环境新增功能
            .AddDevspacesIfNeeded(Configuration.GetValue("EnableDevspaces", false))
            // 添加证书 idsrv3test.pfx 本地文件
            .AddSigningCredential(Certificate.Get())
            // 用户数据
            .AddAspNetIdentity<ApplicationUser>()
            // 配置数据 ConfigurationDbContext 上下文
            // 将指定配置数据（客户端和资源）和操作数据（令牌，代码和和用户的授权信息consents）基于EF进行持久化
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
            })
            // 持久化授权数据 PersistedGrantDbContext 上下文
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
            })
            // 配置数据服务，获取请求上下文的数据
            .Services.AddTransient<IProfileService, ProfileService>();
            #endregion

            // 添加mvc服务的三个子服务services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddControllers();
            services.AddControllersWithViews();
            services.AddRazorPages();

            //初始化 Autofac.ContainerBuilder 类的新实例
            var container = new ContainerBuilder();
            container.Populate(services);

            // autofac 容器化
            return new AutofacServiceProvider(container.Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();
            //loggerFactory.AddAzureWebAppDiagnostics();
            //loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Trace);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            // 配置基础路径
            var pathBase = Configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                //loggerFactory.CreateLogger<Startup>().LogDebug("Using PATH BASE '{pathBase}'", pathBase);
                //添加一个中间件，从请求路径中提取指定的路径库并将其添加到请求路径库中
                app.UsePathBase(pathBase);
            }

            app.UseStaticFiles();

            // 使工作身份服务器重定向在edge和最新版本的浏览器。警告:在生产环境中无效。
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Content-Security-Policy", "script-src 'unsafe-inline'");
                await next();
            });
            // 转发标头
            app.UseForwardedHeaders();
            // 添加 IdentityServer4 服务中间件
            app.UseIdentityServer();

            //修复一个问题与chrome。Chrome启用了一个新功能“没有相同的cookie必须是安全的”，cookie应该从https中删除，但在eShop中，aks和docker的内部共通之处是http。为了避免这个问题，cookies的策略应该是宽松的。
            app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = AspNetCore.Http.SameSiteMode.Lax });
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
                {
                    Predicate = r => r.Name.Contains("self")
                });
            });
        }

        /// <summary>
        /// 性能监控管理服务
        /// </summary>
        /// <param name="services"></param>
        private void RegisterAppInsights(IServiceCollection services)
        {
            //Application Insights 是 Azure Monitor 的一项功能，是面向开发人员和 DevOps 专业人员的可扩展应用程序性能管理 (APM) 服务。 使用它可以监视实时应用程序。 它将自动检测性能异常，并且包含了强大的分析工具来帮助诊断问题，了解用户在应用中实际执行了哪些操作。
            services.AddApplicationInsightsTelemetry(Configuration);
            //K8s，同理
            services.AddApplicationInsightsKubernetesEnricher();
        }
    }
}
