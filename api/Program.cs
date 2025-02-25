using api.Interfaces.Services;
using api.Services;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Azure;
using DotNetEnv;
using api.Data;
using Microsoft.EntityFrameworkCore;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var storageAccountName = builder.Configuration["STORAGE_ACCOUNT_NAME"];


builder.Services.AddAzureClients(
    clientBuilder =>
    {
        clientBuilder.AddBlobServiceClient(new Uri($"https://{storageAccountName}.blob.core.windows.net"));
        clientBuilder.UseCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeAzureCliCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeInteractiveBrowserCredential = true
        }));
    }
);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins(["http://localhost:3000", "https://snappshare.vercel.app"])
                                .AllowAnyMethod()  
                                .AllowAnyHeader()  
                                .AllowCredentials();
                      });
});

builder.Services.AddSingleton<IBlobService, BlobService>();

string dbFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SnappShare");
string dbPath = Path.Combine(dbFolderPath, "snappshare.db");

// Ensure the database folder exists
if (!Directory.Exists(dbFolderPath))
{
    Directory.CreateDirectory(dbFolderPath);
}

builder.Services.AddDbContext<SnappshareContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});


builder.Services.AddControllers(
    options =>
    {
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
    }
);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors(MyAllowSpecificOrigins);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SnappshareContext>();
    dbContext.Database.Migrate();
}


app.Run();
