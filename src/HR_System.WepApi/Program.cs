using System.Text;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.ExtensionMethods;
using HR_System.Infrastructure;
using HR_System.Infrastructure.BackGroundServices;
using HR_System.MiddleWares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// add ApplicationDbContext to services
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Add jwt bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // validation parameters
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer"),
            ValidateAudience = true,
            ValidAudience = builder.Configuration.GetValue<string>("Jwt:Audience"),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("Jwt:Key")!)),
            ValidateLifetime = true
        };

        options.Events = new JwtBearerEvents
        {
            // Get Token From Cookies
            OnMessageReceived = context =>
            {
                //if () TODO
                //{
                //   context.Token = token
                //}
                
                return  Task.CompletedTask;
            }
        };
    });


// implementing serilog
builder.Host.UseSerilog((HostBuilderContext context, IServiceProvider service, LoggerConfiguration configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(service);
});


// add identity services and store users,roles in DBF
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // user password attributes
        options.Password.RequiredLength = 8;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredUniqueChars = 1;
    })
    .AddRoles<ApplicationRole>()
    // decide who is the DB
    .AddEntityFrameworkStores<ApplicationDbContext>();


// DI ---------------------------------

builder.Services.AddOpenApi();
builder.Services.AddApplicationServices();
builder.Services.AddApplicationRepositories();
builder.Services.AddHostedRemoveExpiredRefreshTokens();
builder.Services.AddHttpContextAccessor();

// DI ---------------------------------

var app = builder.Build();

app.UseHsts();
app.UseHttpsRedirection();

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();

    app.MapScalarApiReference();
}

app.UseGlobalExceptionMiddleWare();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();