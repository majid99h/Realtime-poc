using TrafficRealtime.Poc.Api.Hubs;
using TrafficRealtime.Poc.Api.Models;
using TrafficRealtime.Poc.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<RealtimeInstanceSettings>(
    builder.Configuration.GetSection("RealtimeInstance"));
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMqSettings"));

builder.Services.Configure<List<RabbitMqChannelSettings>>(
    options =>
    {
        builder.Configuration
            .GetSection("RabbitMqChannels")
            .Bind(options);
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddSignalR();
;
builder.Services.AddHostedService<RabbitMqConsumerService>();
builder.Services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
builder.Services.AddSingleton<
    IRealtimeDispatcher,
    RealtimeDispatcher>();
builder.Services.AddHostedService<RabbitMqConsumerService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors("AngularClient");
app.MapControllers();

app.MapHub<TrafficHub>("/hubs/traffic");

app.Run();