// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Logn;
using Logn.Components;
using Logn.Core.Flows;
using Logn.Flow;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = LognConstants.AuthenticationScheme;
        options.DefaultSignInScheme = LognConstants.AuthenticationScheme;
    }).AddCookie(LognConstants.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
    {
        o.Authority = builder.Configuration["Logn:Authority"];
        o.RequireHttpsMetadata = true;
        o.TokenValidationParameters.ValidateIssuerSigningKey = true;
        o.TokenValidationParameters.ValidateIssuer = true;
        
        // todo: support audience?
        o.TokenValidationParameters.ValidateAudience = false;
        o.Events = new JwtBearerEvents()
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder().AddPolicy("UserInformation",
    policyBuilder => policyBuilder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser());

builder.Services.AddHttpClient<LognClient>();
builder.Services.AddOptions()
    .Configure<LognOptions>(builder.Configuration.GetSection("Logn"));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSingleton<AuthorizationRepository>();
builder.Services.AddSingleton<ClientStore>();
builder.Services.AddSingleton<AccessTokenBuilder>();
builder.Services.AddLognFlow(o =>
{
    o.AddWorkflow<RequestTokenFlow>(nameof(RequestTokenFlow));
    o.AddWorkflow<ClientCredentialsFlow>(nameof(ClientCredentialsFlow));
});

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<LognOptions>>();
app.UseLogn(options.Value);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();