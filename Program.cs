using Islemler.Components;

using ISLEMLER.Events;

using ISLEMLER.Services;

using Microsoft.AspNetCore.Components.Web;

using Microsoft.OpenApi.Models;

using Serilog;

using Serilog.Events;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;

using System.Text;



var builder = WebApplication.CreateBuilder(args);



// Add services to the container.

builder.Services.AddRazorComponents()

    .AddInteractiveServerComponents();



// Add Swagger services

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>

{

    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    

    // JWT desteği için security tanımı

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme

    {

        Description = "JWT Authorization header using the Bearer scheme.\r\n\r\n" +

                     "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +

                     "Example: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",

        Name = "Authorization",

        In = ParameterLocation.Header,

        Type = SecuritySchemeType.ApiKey,

        BearerFormat = "JWT",

        Scheme = "Bearer"

    });



    c.AddSecurityRequirement(new OpenApiSecurityRequirement

    {

        {

            new OpenApiSecurityScheme

            {

                Reference = new OpenApiReference

                {

                    Type = ReferenceType.SecurityScheme,

                    Id = "Bearer"

                }

            },

            Array.Empty<string>()

        }

    });

});



// Add controller services

builder.Services.AddControllers();

builder.Services.AddScoped<UserService>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);



// Loglama servisini ekleyin

builder.Logging.ClearProviders();

builder.Logging.AddConsole();

builder.Logging.AddDebug();



// Add HTTP client

builder.Services.AddHttpClient();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001") });



// Add Authentication and JWT

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

    .AddJwtBearer(options =>

    {

        var jwtKey = builder.Configuration["Jwt:Key"];

        if (string.IsNullOrEmpty(jwtKey))

        {

            throw new InvalidOperationException("JWT Key is not configured");

        }



        options.SaveToken = true;

        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters

        {

            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),

            ValidateIssuer = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,

            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero

        };

    });



// Add Authorization

builder.Services.AddAuthorization();



// Serilog yapılandırması

Log.Logger = new LoggerConfiguration()

    .MinimumLevel.Debug()

    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)

    .Enrich.FromLogContext()

    .WriteTo.Console()

    .WriteTo.File("logs/log-.txt", 

        rollingInterval: RollingInterval.Day,

        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.ffffff+03}] [{Level:u3}] [{ThreadId}] [{EnvironmentName}] {Message:lj}{NewLine}{Exception}")

    .CreateLogger();



builder.Host.UseSerilog();



var app = builder.Build();



// Configure the HTTP request pipeline.

if (!app.Environment.IsDevelopment())

{

    app.UseExceptionHandler("/Error");

    app.UseHsts();

}

else

{

    app.UseDeveloperExceptionPage();

}



app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();



// Add custom middleware

app.Use(async (context, next) =>

{

    // Log incoming request

    Log.Information(

        "Request {Method} {Url} started at {Time}",

        context.Request.Method,

        context.Request.Path,

        DateTime.UtcNow);



    try

    {

        await next();



        // Log response status code

        Log.Information(

            "Request {Method} {Url} completed with status {StatusCode} at {Time}",

            context.Request.Method,

            context.Request.Path,

            context.Response.StatusCode,

            DateTime.UtcNow);

    }

    catch (Exception ex)

    {

        // Log any unhandled exceptions

        Log.Error(

            ex,

            "Request {Method} {Url} failed with error: {Error}",

            context.Request.Method,

            context.Request.Path,

            ex.Message);

        throw;

    }

});



// Add JWT token validation middleware

app.Use(async (context, next) =>

{

    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

    if (!string.IsNullOrEmpty(token))

    {

        Log.Information(

            "JWT token received for request {Method} {Url}",

            context.Request.Method,

            context.Request.Path);

    }

    await next();

});



// Enable Swagger

app.UseSwagger();

app.UseSwaggerUI(c =>

{

    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");

    c.RoutePrefix = string.Empty; // Swagger UI'ı root URL'de göster

});



app.UseAuthentication();

app.UseAuthorization();



app.MapControllers();

app.MapRazorComponents<App>()

    .AddInteractiveServerRenderMode();



try

{

    app.Logger.LogInformation("Uygulama başlatılıyor...");

    app.Run();

}

catch (Exception ex)

{

    app.Logger.LogCritical(ex, "Uygulama beklenmedik şekilde sonlandı");

}

finally

{

    Log.CloseAndFlush();

}


