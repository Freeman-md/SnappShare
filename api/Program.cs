using api.Interfaces.Services;
using api.Services;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Azure;
using DotNetEnv;
using api.Data;
using api.Extensions;
using Microsoft.EntityFrameworkCore;
using api.Interfaces.Repositories;
using api.Repositories;
using api.Configs;
using Microsoft.Extensions.Options;
using api.Tests.Interfaces.Services;
using System.Text.Json.Serialization;
using api.Middlewares;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.Key));
builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection(ServiceBusOptions.Key));

builder.Services
            .AddBlobStorage(builder.Configuration)
            .AddServiceBus(builder.Configuration);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins([
                            "http://localhost:3000", 
                            "http://localhost:8080", 
                            "https://snappshare.vercel.app", 
                            "https://77e21a22-11e6-4ffc-8fd7-3066161c4a7c.lovableproject.com",
                            "https://32b87998-ed2e-46ad-9f2a-4dfb5c5fea07.lovableproject.com",
                            "https://id-preview--32b87998-ed2e-46ad-9f2a-4dfb5c5fea07.lovable.app",
                            "https://preview--chunked-file-uploader.lovable.app",
                            "https://chunked-file-uploader.vercel.app",
                            ])
                                .AllowAnyMethod()  
                                .AllowAnyHeader()  
                                .AllowCredentials();
                      });
});

builder.Services.AddSingleton<IBlobService, BlobService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFileEntryService, FileEntryService>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IFileEntryRepository, FileEntryRepository>();
builder.Services.AddScoped<IChunkRepository, ChunkRepository>();

builder.Services.AddDbContext<SnappshareContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Snappshare"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));


builder.Services.AddControllers(
    options =>
    {
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
    }
).AddJsonOptions(options => {
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
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

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SnappshareContext>();
    
    if (app.Environment.IsDevelopment())
    {
        // dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
    }
}


app.Run();
