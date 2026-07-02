using HR_System;
using HR_System.Core;
using HR_System.Infrastructure;
using HR_System.MiddleWares;
using HR_System.SignalR.Messages;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// implementing serilog
builder.Host.UseSerilog((HostBuilderContext context, IServiceProvider service, LoggerConfiguration configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(service);
});

// DI ---------------------------------

builder.Services.AddCore()
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
app.UseCors("Angular");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MessagesHub>("/hubs/messages");

app.Run();