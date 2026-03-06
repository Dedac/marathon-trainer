using Microsoft.EntityFrameworkCore;
using MarathonTrainer.Api.Data;
using MarathonTrainer.Api.Middleware;
using MarathonTrainer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddOpenApi();

builder.Services.AddScoped<ITrainingPlanGenerator, TrainingPlanGenerator>();
builder.Services.AddScoped<IMedicalAdjustmentService, MedicalAdjustmentService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=marathon-trainer.db"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure the database is created and migrations are applied on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowAngularDev");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
