using HR_System;
using HR_System.Core;
using HR_System.Infrastructure;
using HR_System.MiddleWares;
using HR_System.SignalR.App;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// implementing serilog
builder.Host.UseSerilog((context, service, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(service);
});

// DI ---------------------------------

builder.Services
    .AddCore()
    .AddInfrastructure(builder.Configuration)
    .AddWebApi(builder.Configuration);

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
app.UseCors("Angular");   // after UseRouting and before auth, so preflight requests get answered
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<AppHub>("/hubs/app");

app.Run();
