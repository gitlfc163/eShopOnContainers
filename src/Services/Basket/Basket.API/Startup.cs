namespace Microsoft.eShopOnContainers.Services.Basket.API;
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public virtual IServiceProvider ConfigureServices(IServiceCollection services)
    {
        //Grpc服务
        services.AddGrpc(options =>
        {
            // 获取或设置一个值，该值指示是否向对等方发送详细的错误消息。
            // 详细的错误消息包括服务器上抛出的异常的详细信息。
            options.EnableDetailedErrors = true;
        });

        // 注册App监控
        RegisterAppInsights(services);

        services.AddControllers(options =>
            {
                // 全局异常过滤器
                options.Filters.Add(typeof(HttpGlobalExceptionFilter));
                // 模型验证过滤器
                options.Filters.Add(typeof(ValidateModelStateFilter));

            }) 
            // 功能集成测试
            .AddApplicationPart(typeof(BasketController).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);

        #region Swagger
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "eShopOnContainers - Basket HTTP API",
                Version = "v1",
                Description = "The Basket Service HTTP API"
            });

            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows()
                {
                    Implicit = new OpenApiOAuthFlow()
                    {
                        AuthorizationUrl = new Uri($"{Configuration.GetValue<string>("IdentityUrlExternal")}/connect/authorize"),
                        TokenUrl = new Uri($"{Configuration.GetValue<string>("IdentityUrlExternal")}/connect/token"),
                        Scopes = new Dictionary<string, string>()
                        {
                            { "basket", "Basket API" }
                        }
                    }
                }
            });
            // 授权检查操作过滤器
            options.OperationFilter<AuthorizeCheckOperationFilter>();
        });

        #endregion

        // 配置认证服务
        ConfigureAuthService(services);

        // 自定义健康检测
        services.AddCustomHealthCheck(Configuration);

        // 获取配置数据
        services.Configure<BasketSettings>(Configuration);

        #region Redis
        //配置启动Redis服务，虽然可能影响项目启动速度，但是不用在运行的时候报错，所以是合理的.
        services.AddSingleton<ConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<BasketSettings>>().Value;
            var configuration = ConfigurationOptions.Parse(settings.ConnectionString, true);

            // 返回redis的连接实例;ConnectionMultiplexer表示到 redis 服务器的一组相互关联的连接。
            return ConnectionMultiplexer.Connect(configuration);
        });

        #endregion

        #region 服务总线，Azure或RabbitMQ
        // Azure 服务总线是否启用
        if (Configuration.GetValue<bool>("AzureServiceBusEnabled"))
        {
            // 服务总线连接
            services.AddSingleton<IServiceBusPersisterConnection>(sp =>
            {
                var serviceBusConnectionString = Configuration["EventBusConnection"];

                return new DefaultServiceBusPersisterConnection(serviceBusConnectionString);
            });
        }
        else
        {
            //注册IRabbitMQPersistentConnection服务用于设置RabbitMQ连接
            services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();

                var factory = new ConnectionFactory()
                {
                    HostName = Configuration["EventBusConnection"],
                    DispatchConsumersAsync = true
                };

                if (!string.IsNullOrEmpty(Configuration["EventBusUserName"]))
                {
                    factory.UserName = Configuration["EventBusUserName"];
                }

                if (!string.IsNullOrEmpty(Configuration["EventBusPassword"]))
                {
                    factory.Password = Configuration["EventBusPassword"];
                }

                var retryCount = 5;
                if (!string.IsNullOrEmpty(Configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(Configuration["EventBusRetryCount"]);
                }

                return new DefaultRabbitMQPersistentConnection(factory, logger, retryCount);
            });
        }

        #endregion

        // 注册事件总线 有更新价格、订单生成两部分
        RegisterEventBus(services);

        // 跨域
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                builder => builder
                .SetIsOriginAllowed((host) => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
        });

        // 从Httpcontext中获取指定信息
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        // 仓储数据
        services.AddTransient<IBasketRepository, RedisBasketRepository>();
        services.AddTransient<IIdentityService, IdentityService>();

        services.AddOptions();

        // Autofac容器，因为用的是webhost，不是host泛型宿主，所以可以这么写
        var container = new ContainerBuilder();
        container.Populate(services);

        return new AutofacServiceProvider(container.Build());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        //loggerFactory.AddAzureWebAppDiagnostics();
        //loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Trace);

        var pathBase = Configuration["PATH_BASE"];
        if (!string.IsNullOrEmpty(pathBase))
        {
            app.UsePathBase(pathBase);
        }

        app.UseSwagger()
            .UseSwaggerUI(setup =>
            {
                setup.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "Basket.API V1");
                setup.OAuthClientId("basketswaggerui");
                setup.OAuthAppName("Basket Swagger UI");
            });

        app.UseRouting();
        app.UseCors("CorsPolicy");
        ConfigureAuth(app);

        app.UseStaticFiles();

        #region 结点路由
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<BasketService>();
            endpoints.MapDefaultControllerRoute();
            endpoints.MapControllers();
            endpoints.MapGet("/_proto/", async ctx =>
            {
                ctx.Response.ContentType = "text/plain";
                using var fs = new FileStream(Path.Combine(env.ContentRootPath, "Proto", "basket.proto"), FileMode.Open, FileAccess.Read);
                using var sr = new StreamReader(fs);
                while (!sr.EndOfStream)
                {
                    var line = await sr.ReadLineAsync();
                    if (line != "/* >>" || line != "<< */")
                    {
                        await ctx.Response.WriteAsync(line);
                    }
                }
            });
            #region 健康检测
            endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
            {
                Predicate = r => r.Name.Contains("self")
            });
            #endregion
        }); 
        #endregion

        // 配置事件总线（订单和产品价格）
        ConfigureEventBus(app);
    }

    /// <summary>
    /// 注册App监控
    /// </summary>
    /// <param name="services"></param>
    private void RegisterAppInsights(IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetry(Configuration);
        //Kubernetes注册App监控
        services.AddApplicationInsightsKubernetesEnricher();
    }

    /// <summary>
    /// 在启动类进行认证中间件的配置
    /// 购物车管理界面是需要认证和授权。那自然需要与上游的Identity Microservice进行衔接
    /// </summary>
    /// <param name="services"></param>
    private void ConfigureAuthService(IServiceCollection services)
    {
        // prevent from mapping "sub" claim to nameidentifier.
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

        var identityUrl = Configuration.GetValue<string>("IdentityUrl");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

        }).AddJwtBearer(options =>
        {
            options.Authority = identityUrl;
            options.RequireHttpsMetadata = false;
            options.Audience = "basket";
        });
    }

    protected virtual void ConfigureAuth(IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    /// <summary>
    /// 注册事件总线
    /// 有更新价格、订单生成两部分
    /// </summary>
    /// <param name="services"></param>
    private void RegisterEventBus(IServiceCollection services)
    {
        if (Configuration.GetValue<bool>("AzureServiceBusEnabled"))
        {
            services.AddSingleton<IEventBus, EventBusServiceBus>(sp =>
            {
                var serviceBusPersisterConnection = sp.GetRequiredService<IServiceBusPersisterConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusServiceBus>>();
                var eventBusSubscriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
                string subscriptionName = Configuration["SubscriptionClientName"];

                return new EventBusServiceBus(serviceBusPersisterConnection, logger,
                    eventBusSubscriptionsManager, iLifetimeScope, subscriptionName);
            });
        }
        else
        {
            //注册单例模式的EventBusRabbitMQ
            services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
            {
                var subscriptionClientName = Configuration["SubscriptionClientName"];
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ>>();
                var eventBusSubscriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                //事件总线重试计数
                var retryCount = 5;
                if (!string.IsNullOrEmpty(Configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(Configuration["EventBusRetryCount"]);
                }

                return new EventBusRabbitMQ(rabbitMQPersistentConnection, logger, iLifetimeScope, eventBusSubscriptionsManager, subscriptionClientName, retryCount);
            });
        }
        //注册单例模式的IEventBusSubscriptionsManager用于订阅管理
        services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

        services.AddTransient<ProductPriceChangedIntegrationEventHandler>();
        services.AddTransient<OrderStartedIntegrationEventHandler>();
    }
    /// <summary>
    /// 事件订阅
    /// </summary>
    /// <param name="app"></param>
    private void ConfigureEventBus(IApplicationBuilder app)
    {
        // 事件订阅
        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
        // 商品价格修改事件
        eventBus.Subscribe<ProductPriceChangedIntegrationEvent, ProductPriceChangedIntegrationEventHandler>();
        // 订单启动事件
        eventBus.Subscribe<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>();
    }
}