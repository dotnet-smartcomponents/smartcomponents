// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Primitives;
using SmartComponents.Inference;
using SmartComponents.Inference.OpenAI;
using SmartComponents.LocalEmbeddings;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddRepoSharedConfig();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSmartComponents()
    .WithInferenceBackend<OpenAIInferenceBackend>();

builder.Services.AddSingleton<LocalEmbedder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Prepare a list of expense categories and corresponding embeddings
var embedder = app.Services.GetRequiredService<LocalEmbedder>();
var expenseCategories = embedder.EmbedRange(
    ["Groceries", "Utilities", "Rent", "Mortgage", "Car Payment", "Car Insurance", "Health Insurance", "Life Insurance", "Home Insurance", "Gas", "Public Transportation", "Dining Out", "Entertainment", "Travel", "Clothing", "Electronics", "Home Improvement", "Gifts", "Charity", "Education", "Childcare", "Pet Care", "Other"]);

app.MapSmartComboBox("/api/suggestions/accounting-categories",
    request =>
    {
        StringValues minSimilarity;
        request.HttpContext.Request.Form.TryGetValue("similarityThreshold", out minSimilarity);

        var query = new SimilarityQuery()
        {

            SearchText = request.Query.SearchText,
            MaxResults = request.Query.MaxResults,
            MinSimilarity = Convert.ToSingle(minSimilarity.ToString(), new CultureInfo("en-US"))
        };
        return embedder.FindClosest(query, expenseCategories);
    });

app.Run();
