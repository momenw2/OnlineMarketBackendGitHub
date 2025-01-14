using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using PostgreSQL.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DbContext for PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebApiDatabase")));

// 2. Configure Authentication with JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "your-issuer",
        ValidAudience = "your-audience",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-super-secret-key-that-is-long-enough"))
    };
});

// 3. Add Authorization middleware
builder.Services.AddAuthorization();

// 4. Add CORS policy
builder.Services.AddCors(options =>
{

    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:3001") // Replace with your frontend port
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });

});


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


// 6. Add Swagger with Bearer token configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OnlineMarketApi", Version = "v1" });



    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter your JWT with Bearer prefix (Bearer {token})",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });

    c.OperationFilter<PlainTextOperationFilter>();
});

// 7. Build the app
var app = builder.Build();

// 8. Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseCors();
app.UseAuthentication();        // Use authentication
app.UseAuthorization();         // Use authorization

app.MapControllers();

app.Run();

// Operation filter for Swagger
public class PlainTextOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.RouteValues["action"] == "GetProfile")
        {
            if (!operation.Responses["200"].Content.ContainsKey("text/plain"))
            {
                operation.Responses["200"].Content.Add("text/plain", new OpenApiMediaType());
            }
        }
        else
        {
            if (!operation.Responses["200"].Content.ContainsKey("application/json"))
            {
                operation.Responses["200"].Content.Add("application/json", new OpenApiMediaType());
            }
        }
    }
}
