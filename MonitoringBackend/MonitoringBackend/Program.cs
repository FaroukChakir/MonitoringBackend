using MonitoringBackend.Configuration;
using MonitoringBackend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);




// adding services to the container.
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MonitoringDB")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/*************************************************/
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApiDbContext>();

var key = Encoding.ASCII.GetBytes(builder.Configuration.GetSection("JwtConfig:Secret").Value);

//var tokenValidationparamaeter = new TokenValidationParameters()
//{
//    ValidateIssuerSigningKey = true,
//    IssuerSigningKey = new SymmetricSecurityKey(key),
//    ValidateIssuer = false,
//    ValidateAudience = false,
//    ValidIssuer = "https://localhost:7044/",
//    RequireExpirationTime = true,
//    ValidateLifetime = true,
//    ClockSkew = TimeSpan.Zero
//};

var tokenValidationParameters = new TokenValidationParameters()
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ValidateIssuer = false,
    ValidateAudience = false,
    ValidIssuer = "https://localhost:7044/",
    RequireExpirationTime = true,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero
};

builder.Services.AddSingleton(tokenValidationParameters);


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

})
    .AddJwtBearer(jwt =>
    {
        jwt.SaveToken = true;
        jwt.TokenValidationParameters = tokenValidationParameters;

    });

/******* CORS  ******/

builder.Services.AddCors(options=>options.AddPolicy("FrontEnd", policy=>{
    policy.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader();
}));

/*************************************************/
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
/**********************************/
app.UseAuthentication();
app.UseCors("FrontEnd");

/***********************************/

app.UseAuthorization();


app.MapControllers();

app.Run();
