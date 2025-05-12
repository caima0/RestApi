using api.Data;
using api.Interfaces;
using api.Repository;
using api.Service;
using api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options=>
        {
             options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
             {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Example header: Bearer {token}"

             });
             options.AddSecurityRequirement(new OpenApiSecurityRequirement()
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
                  Array.Empty<string>()
               
                   
                }

             }); 
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(option =>
            {
                var key = Encoding.UTF8.GetBytes(builder.Configuration.GetRequiredSection("JwtSettings")["SecretKey"] ?? throw new ArgumentNullException());
                option.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
            });

        builder.Services.AddDbContext<ApplicationDBContex>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DeafaultConnection"));
        });

        builder.Services.AddScoped<IRateRepository, RateRepository>();
        builder.Services.AddSingleton<IUserMockInterface, UserMockService>();
        builder.Services.AddHttpClient<INBPClient, NBPClient>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
    }
}