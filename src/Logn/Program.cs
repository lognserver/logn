// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using Logn;
using Logn.Components;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = LognConstants.AuthenticationScheme;
    options.DefaultSignInScheme = LognConstants.AuthenticationScheme;
}).AddCookie(LognConstants.AuthenticationScheme);

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<LognClient>();
builder.Services.AddOptions()
    .Configure<LognOptions>(builder.Configuration.GetSection("Logn"));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSingleton<AuthorizationRepository>();

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<LognOptions>>();
app.UseLogn(options.Value);

// Configure the HTTP request pipeline.
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


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();