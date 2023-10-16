using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using MyBlog.Business;
using MyBlog.Business.Email;
using MyBlog.Business.IO;
using MyBlog.Data;
using MyBlog.Web.Infrastructure;
using MyBlog.Web.Infrastructure.Mvc;
using MyBlog.Web.Infrastructure.Mvc.Health;
using MyBlog.Web.Infrastructure.Mvc.SecurityHeaders;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.SetMinimumLevel(LogLevel.Trace);
    logging.AddConsole();
});

builder.Host.UseSerilog();

// 數據保護（提供一個簡單、基於非對稱加密進行的加密API）
builder.Services.AddDataProtection()
            // 以本地文件形式保存私钥信息
            .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["PersistKeysFile:path"]))
            // 私钥有效期限（默认90天）
            .SetDefaultKeyLifetime(TimeSpan.FromDays(15))
            // 数据保护系统的隔离标识。（即：如果应用程序之间共享保护负载，那么每个应用中需要使用相同的值配置 ApplicationName）
            .SetApplicationName("my_app_sample_mvc");

// 配置信息綁定結構類並注入容器
builder.Services.Configure<BlogSettings>(builder.Configuration.GetSection("BlogSettings"));
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

// 配置 cookie 策略(全局设定)，以便在追加或删除 cookie 时调用帮助程序类
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // 是否需要用户确认才能将部分cookie发送到客户端（默认值false）
    options.CheckConsentNeeded = context => true;
    // 支持跨域 Cookie 传递
    options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
    // 如果提供 Cookie 的 URI 是 HTTPS，则只会在后续 HTTPS 请求上将 Cookie 返回到服务器。否则，如果提供 Cookie 的 URI 为 HTTP，则 Cookie 将针对所有 HTTP 和 HTTPS 请求返回到服务器。
    options.Secure = CookieSecurePolicy.SameAsRequest;
    // 设置存储用户登录信息（用户Token信息）的Cookie，无法通过客户端浏览器脚本(如JavaScript等)访问到
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.None;
});

// 數據庫服務註冊
builder.Services.AddDbContext<EFUnitOfWork>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EFUnitOfWork")), ServiceLifetime.Scoped);

// 与 UseDeveloperExceptionPage 结合使用，这会捕获可通过使用 Entity Framework 迁移解决的数据库相关异常。 发生这些异常时，将生成 HTML 响应（展示異常信息頁面）
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 基於 IdentityUser 的自定義 User 類的服務註冊（全局配置設定）
builder.Services.AddDefaultIdentity<User>(config =>
{
    // 登錄錯誤後的鎖定時間
    config.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    // 最大的登錄錯誤次數
    config.Lockout.MaxFailedAccessAttempts = 10;

    // 賬戶設定（Email必須）
    config.User.RequireUniqueEmail = true;

    // 密碼設定（數字不允許、全大 / 小寫字母不允許、特殊符號不允許、密碼長度、相同字符可以出現的次數）
    config.Password.RequireDigit = false;
    config.Password.RequireLowercase = false;
    config.Password.RequireNonAlphanumeric = false;
    config.Password.RequireUppercase = false;
    config.Password.RequiredLength = 6;
    config.Password.RequiredUniqueChars = 1;

})
    // 代碼調用 AddDefaultUI（用於對 Razor 類庫提供 IdentityUser 的擴展支持）
    .AddDefaultUI()
    // 在 Startup.ConfigureServices 中添加 Identity 服務時，需要註冊自定義數據庫上下文
    .AddEntityFrameworkStores<EFUnitOfWork>()
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>();

builder.Services.AddLocalization();

builder.Services.AddControllersWithViews()
    .AddViewLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddTransient<INotificationService, EmailNotificationService>();
builder.Services.AddTransient<IEmailSender, EmailSenderAdapter>();
builder.Services.AddTransient<IImageFileProvider, ImageFileProvider>();
builder.Services.AddTransient<IBlogEntryFileFileProvider, BlogEntryFileFileProvider>();

// Add CommandHandlers with decoractor
// See https://github.com/khellang/Scrutor
foreach (var serviceType in typeof(ICommandHandler<>).Assembly.GetTypes())
{
    if (serviceType.IsAbstract || serviceType.IsInterface || serviceType.BaseType == null)
    {
        continue;
    }

    foreach (var interfaceType in serviceType.GetInterfaces())
    {
        if (interfaceType.IsGenericType && typeof(ICommandHandler<>).IsAssignableFrom(interfaceType.GetGenericTypeDefinition()))
        {
            // Register service
            builder.Services.AddScoped(interfaceType, serviceType);

            break;
        }
    }
}

builder.Services.Decorate(typeof(ICommandHandler<>), typeof(CommandLoggingDecorator<>));

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration["ConnectionStrings:EFUnitOfWork"])
    .AddCheck<LogfileHealthCheck>("Log files");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 24 * 60 * 60; // 24 hours
        ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + durationInSeconds;
    }
});
// 为了能够在管道中获取cookie信息（註冊中間件）
app.UseCookiePolicy();
/*
app.UseCookiePolicy(
    new CookiePolicyOptions
    {
        CheckConsentNeeded = context => true,
        MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None,
        Secure = CookieSecurePolicy.SameAsRequest,
        HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.None,
        OnAppendCookie = (context) =>
        {
            // 是否要将修改后的 cookie 发送到浏览器
            context.IssueCookie = true;

            // 添加cookie后的回调处理
        },
        OnDeleteCookie = (context) =>
        {
            // 删除cookie后的回调处理
        },
    });
*/


app.UseSecurityHeaders(builder =>
{
    builder.PermissionsPolicySettings.Camera.AllowNone();

    builder.CspSettings.Defaults.AllowNone();
    builder.CspSettings.Connect.AllowSelf();
    builder.CspSettings.Manifest.AllowSelf();
    builder.CspSettings.Objects.AllowNone();
    builder.CspSettings.Frame.AllowNone();
    builder.CspSettings.Scripts.AllowSelf();

    builder.CspSettings.Styles
        .AllowSelf()
        .AllowUnsafeInline();

    builder.CspSettings.Fonts.AllowSelf();

    builder.CspSettings.Images
        .AllowSelf()
        .Allow("https://i2.wp.com")
        .Allow("https://www.gravatar.com");

    builder.CspSettings.BaseUri.AllowNone();
    builder.CspSettings.FormAction.AllowSelf();
    builder.CspSettings.FrameAncestors.AllowNone();

    builder.ReferrerPolicy = ReferrerPolicies.NoReferrerWhenDowngrade;
});

var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("de"),
    new CultureInfo("zh-cn")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(supportedCultures[0]),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Blog}/{action=Index}/{id?}");
    endpoints.MapRazorPages();
});

app.UseHealthChecks("/health", new HealthCheckOptions()
{
    ResponseWriter = WriteResponse
});

Log.Logger = new LoggerConfiguration()
       .MinimumLevel.Information()
       .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
       .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Query", Serilog.Events.LogEventLevel.Error)
       .Enrich.FromLogContext()
       .WriteTo.RollingFile("Logs/Log-{Date}.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
       .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level >= Serilog.Events.LogEventLevel.Error).WriteTo.RollingFile("Logs/Errors/ErrorLog-{Date}.txt", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}"))
       .CreateLogger();

using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var context = serviceScope.ServiceProvider.GetService<EFUnitOfWork>();
    //context!.Database.Migrate();
    context!.Database.EnsureCreated();  // 數據庫不存在則創建
}


try
{
    var discriminator = app.Services.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator;
    Log.Information($"ApplicationDiscriminator: {discriminator}");

    Log.Information("Starting application");
    app.Run();
    Log.Information("Stopped application");
    return 0;
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

static Task WriteResponse(HttpContext httpContext, HealthReport result)
{
    var writerOptions = new JsonWriterOptions
    {
        Indented = true
    };

    using (var ms = new MemoryStream())
    {
        using (var writer = new Utf8JsonWriter(ms, options: writerOptions))
        {
            writer.WriteStartObject();
            writer.WriteString("status", result.Status.ToString());
            writer.WriteStartObject("results");

            foreach (var kv in result.Entries)
            {
                writer.WriteStartObject(kv.Key);
                writer.WriteString("status", kv.Value.Status.ToString());
                writer.WriteString("description", kv.Value.Description);

                writer.WriteStartObject("data");
                foreach (var item in kv.Value.Data)
                {
                    writer.WriteString(item.Key, item.Value?.ToString());
                }

                writer.WriteEndObject();

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        httpContext.Response.ContentType = "application/json";
        return httpContext.Response.WriteAsync(Encoding.UTF8.GetString(ms.ToArray()));
    }
}