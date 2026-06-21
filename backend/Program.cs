using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using LocadoraVeiculos.Data;

var builder = WebApplication.CreateBuilder(args);

// ---- Entity Framework + SQL Express ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Controllers ----
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Evita loops de referência ao serializar entidades relacionadas
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// ---- CORS: libera o front-end (HTML/JS) a consumir a API ----
const string CorsPolicy = "PermitirFront";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ---- Swagger / OpenAPI ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Locadora de Veículos",
        Version = "v1",
        Description = "API RESTful para gestão de uma locadora de veículos. " +
                      "Trabalho Prático - PUC Minas."
    });

    // Inclui comentários XML na documentação (gerados a partir dos /// dos controllers)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ---- Cria o banco e aplica migrations automaticamente ao iniciar ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    // Em produção real, usar db.Database.Migrate() com migrations versionadas.
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Locadora API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors(CorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();
