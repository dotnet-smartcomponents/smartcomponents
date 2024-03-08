// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ExampleBlazorApp.Components;
using SmartComponents.Inference.OpenAI;
using SmartComponents.LocalEmbeddings;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddRepoSharedConfig();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSmartComponents()
    .WithInferenceBackend<OpenAIInferenceBackend>();

builder.Services.AddSingleton<LocalEmbedder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

var embedder = app.Services.GetRequiredService<LocalEmbedder>();
var expenseCategories = embedder.EmbedRange(
    ["Groceries", "Utilities", "Rent", "Mortgage", "Car Payment", "Car Insurance", "Health Insurance", "Life Insurance", "Home Insurance", "Gas", "Public Transportation", "Dining Out", "Entertainment", "Travel", "Clothing", "Electronics", "Home Improvement", "Gifts", "Charity", "Education", "Childcare", "Pet Care", "Other"]);

app.MapSmartComboBox("/api/suggestions/expense-category",
    request => embedder.FindClosest(request.Query, expenseCategories));

app.Run();
