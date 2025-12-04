//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.IdentityModel.Tokens;
//using System.Text;
//using WebApplication1.DataAccess;
//using WebApplication1.Models;

//var builder = WebApplication.CreateBuilder(args);

//// 1. Register IHttpContextAccessor
//builder.Services.AddHttpContextAccessor();



//builder.Services.AddScoped<DACategory>();
//builder.Services.AddScoped<DAUser>();
//builder.Services.AddScoped<DACart>();
//builder.Services.AddScoped<DACheckout>();
//builder.Services.AddScoped<DAProduct>();
//builder.Services.AddScoped<DAWishlist>();
//builder.Services.AddScoped<DAReturnRequest>();





//// 3. Add controllers and JSON settings
//builder.Services.AddControllers()
//    .AddNewtonsoftJson(opts =>
//        opts.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

//// 4. JWT Authentication configuration
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var secretKey = jwtSettings["Key"];

//builder.Services
//    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = jwtSettings["Issuer"],
//            ValidAudience = jwtSettings["Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
//        };
//    });

//// 5. Authorization
//builder.Services.AddAuthorization();

//// 6. CORS policy for frontend
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowFrontend", policy =>
//    {
//        policy.WithOrigins("https://liarafashion.dockyardsoftware.com")
//              .AllowAnyMethod()
//              .AllowAnyHeader()
//              .AllowCredentials(); 
//    });
//});

//// 7. Swagger
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(options =>
//{
//    options.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });
//});

//// 8. Build app and configure middleware
//var app = builder.Build();

//// 9. Development tools
//if (app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage();
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseRouting();
//app.UseStaticFiles();

//app.UseCors("AllowFrontend");

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllers();

//app.Run();
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebApplication1.DataAccess;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Register IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 2. Register your DA services
builder.Services.AddScoped<DACategory>();
builder.Services.AddScoped<DAUser>();
builder.Services.AddScoped<DACart>();
builder.Services.AddScoped<DACheckout>();
builder.Services.AddScoped<DAProduct>();
builder.Services.AddScoped<DAWishlist>();
builder.Services.AddScoped<DAReturnRequest>();

// 3. Add controllers with JSON options
builder.Services.AddControllers()
    .AddNewtonsoftJson(opts =>
        opts.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

// 4. JWT Authentication configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Key"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
        };
    });

// 5. Authorization
builder.Services.AddAuthorization();

// ✅ 6. CORS policy - allow frontend domain
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://liarafashion.dockyardsoftware.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 7. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });
});

var app = builder.Build();

// ✅ 8. Global error handler to return JSON with CORS
app.UseExceptionHandler("/error");

// 9. Swagger (only in development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ 10. Apply CORS **before** routing/auth
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
