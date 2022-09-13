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
        //Grpc����
        services.AddGrpc(options =>
        {
            // ��ȡ������һ��ֵ����ֵָʾ�Ƿ���Եȷ�������ϸ�Ĵ�����Ϣ��
            // ��ϸ�Ĵ�����Ϣ�������������׳����쳣����ϸ��Ϣ��
            options.EnableDetailedErrors = true;
        });

        // ע��App���
        RegisterAppInsights(services);

        services.AddControllers(options =>
            {
                // ȫ���쳣������
                options.Filters.Add(typeof(HttpGlobalExceptionFilter));
                // ģ����֤������
                options.Filters.Add(typeof(ValidateModelStateFilter));

            }) 
            // ���ܼ��ɲ���
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
            // ��Ȩ������������
            options.OperationFilter<AuthorizeCheckOperationFilter>();
        });

        #endregion

        // ������֤����
        ConfigureAuthService(services);

        // �Զ��彡�����
        services.AddCustomHealthCheck(Configuration);

        // ��ȡ��������
        services.Configure<BasketSettings>(Configuration);

        #region Redis
        //��������Redis������Ȼ����Ӱ����Ŀ�����ٶȣ����ǲ��������е�ʱ�򱨴������Ǻ����.
        services.AddSingleton<ConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<BasketSettings>>().Value;
            var configuration = ConfigurationOptions.Parse(settings.ConnectionString, true);

            // ����redis������ʵ��;ConnectionMultiplexer��ʾ�� redis ��������һ���໥���������ӡ�
            return ConnectionMultiplexer.Connect(configuration);
        });

        #endregion

        #region �������ߣ�Azure��RabbitMQ
        // Azure ���������Ƿ�����
        if (Configuration.GetValue<bool>("AzureServiceBusEnabled"))
        {
            // ������������
            services.AddSingleton<IServiceBusPersisterConnection>(sp =>
            {
                var serviceBusConnectionString = Configuration["EventBusConnection"];

                return new DefaultServiceBusPersisterConnection(serviceBusConnectionString);
            });
        }
        else
        {
            //ע��IRabbitMQPersistentConnection������������RabbitMQ����
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

        // ע���¼����� �и��¼۸񡢶�������������
        RegisterEventBus(services);

        // ����
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                builder => builder
                .SetIsOriginAllowed((host) => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
        });

        // ��Httpcontext�л�ȡָ����Ϣ
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        // �ִ�����
        services.AddTransient<IBasketRepository, RedisBasketRepository>();
        services.AddTransient<IIdentityService, IdentityService>();

        services.AddOptions();

        // Autofac��������Ϊ�õ���webhost������host�������������Կ�����ôд
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

        #region ���·��
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
            #region �������
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

        // �����¼����ߣ������Ͳ�Ʒ�۸�
        ConfigureEventBus(app);
    }

    /// <summary>
    /// ע��App���
    /// </summary>
    /// <param name="services"></param>
    private void RegisterAppInsights(IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetry(Configuration);
        //Kubernetesע��App���
        services.AddApplicationInsightsKubernetesEnricher();
    }

    /// <summary>
    /// �������������֤�м��������
    /// ���ﳵ�����������Ҫ��֤����Ȩ������Ȼ��Ҫ�����ε�Identity Microservice�����ν�
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
    /// ע���¼�����
    /// �и��¼۸񡢶�������������
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
            //ע�ᵥ��ģʽ��EventBusRabbitMQ
            services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
            {
                var subscriptionClientName = Configuration["SubscriptionClientName"];
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ>>();
                var eventBusSubscriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                //�¼��������Լ���
                var retryCount = 5;
                if (!string.IsNullOrEmpty(Configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(Configuration["EventBusRetryCount"]);
                }

                return new EventBusRabbitMQ(rabbitMQPersistentConnection, logger, iLifetimeScope, eventBusSubscriptionsManager, subscriptionClientName, retryCount);
            });
        }
        //ע�ᵥ��ģʽ��IEventBusSubscriptionsManager���ڶ��Ĺ���
        services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

        services.AddTransient<ProductPriceChangedIntegrationEventHandler>();
        services.AddTransient<OrderStartedIntegrationEventHandler>();
    }
    /// <summary>
    /// �¼�����
    /// </summary>
    /// <param name="app"></param>
    private void ConfigureEventBus(IApplicationBuilder app)
    {
        // �¼�����
        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
        // ��Ʒ�۸��޸��¼�
        eventBus.Subscribe<ProductPriceChangedIntegrationEvent, ProductPriceChangedIntegrationEventHandler>();
        // ���������¼�
        eventBus.Subscribe<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>();
    }
}