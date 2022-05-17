using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

#region WebApplicationBuilder
var builder = WebApplication.CreateBuilder(args);

builder.Logging
    //.SetMinimumLevel(LogLevel.Information)
    .AddFilter("DemoNetCoreMinimal", LogLevel.Trace)
    .AddFilter("Microsoft", LogLevel.Information)
    .AddFilter("System", LogLevel.Information)
    .AddConsole();

#region Services
builder.Services.AddScoped<Service>();
#endregion

builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.IncludeFields = true;
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});
#endregion

#region WebApplication
var app = builder.Build();

#region 中介軟體
//名稱                                  描述                          API
//Authentication                        認證中介軟體                  app.UseAuthentication()
//Authorization                         授權中介軟體.                 app.UseAuthorization()
//CORS                                  跨域中介軟體.                 app.UseCors()
//Exception Handler                     全域性異常處理中介軟體.       app.UseExceptionHandler()
//Forwarded Headers                     代理頭資訊轉發中介軟體.       app.UseForwardedHeaders()
//HTTPS Redirection                     Https重定向中介軟體.          app.UseHttpsRedirection()
//HTTP Strict Transport Security (HSTS) 特殊響應頭的安全增強中介軟體. app.UseHsts()
//Request Logging                       HTTP請求和響應日誌中介軟體.   app.UseHttpLogging()
//Response Caching                      輸出快取中介軟體.             app.UseResponseCaching()
//Response Compression                  響應壓縮中介軟體.             app.UseResponseCompression()
//Session                               Session中介軟體               app.UseSession()
//Static Files                          靜態檔案中介軟體.             app.UseStaticFiles(), app.UseFileServer()
//WebSockets                            WebSocket支援中介軟體.        app.UseWebSockets()
#endregion

app.UseDeveloperExceptionPage();
app.UseHttpLogging();

app.UseMiddleware<DefaultMiddleware>();

#region Map
// root
app.MapGet("/", async (HttpContext context) => 
{
    app.Logger.LogInformation("WebApplication Running!");
    return await Task.FromResult("WebApplication Running!");
});
app.MapGet("/{id}", async (HttpContext context,
    [FromHeader(Name = "Content-Type")] string? contentType,
    [FromRoute] int? id,
    [FromQuery(Name = "text")] string? text,
    [FromServices] Service service) => 
{
    app.Logger.LogInformation(JsonSerializer.Serialize(contentType));
    app.Logger.LogInformation(JsonSerializer.Serialize(id));
    app.Logger.LogInformation(JsonSerializer.Serialize(text));
    await service.Run();
    return await Task.FromResult(Results.Ok());
});
// test
app.MapGet("/ValueFromQuery", async (HttpContext context, 
    [FromQuery(Name = "model")] string model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    return await Task.FromResult(Results.Ok(model));
});
app.MapPost("/ValueFromBody", async (HttpContext context, 
    [FromBody] string model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    return await Task.FromResult(Results.Ok(model));
});
app.MapGet("/JsonFromQuery", async  (HttpContext context, 
    [FromQuery(Name = "text")] string? text, 
    [FromQuery(Name = "value")] int? value, 
    [FromQuery(Name = "date")] DateTime? date) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(text));
    app.Logger.LogInformation(JsonSerializer.Serialize(value));
    app.Logger.LogInformation(JsonSerializer.Serialize(date));
    return await Task.FromResult(Results.Ok(new { text, value, date }));
});
app.MapPost("/JsonFromBody", async (HttpContext context, 
    [FromBody] JsonDto model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    return await Task.FromResult(Results.Ok(model));
});
app.MapPost("/Upload", async (HttpContext context) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(context.Request.Form.Files));
    app.Logger.LogInformation(JsonSerializer.Serialize(context.Request.Form.ToList()));
    return await Task.FromResult(Results.Ok());
});
app.MapGet("/Download", async (HttpContext context) =>
{
    var source = "d:/workspace/Download.zip";
    if (File.Exists(source))
    {
        context.Response.ContentType = "application/download";
        context.Response.Headers.Add("content-disposition", $"attachment; filename=Download.zip");
        await context.Response.SendFileAsync(source);
        return await Task.FromResult(Results.Ok());
    }
    else
    {
        return await Task.FromResult(Results.Ok("File Not Exists"));
    }
});
// login
app.MapPost("/SignIn", async (HttpContext context, 
    [FromBody] SignInDto model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    if (!string.IsNullOrEmpty(model.Account) && !string.IsNullOrEmpty(model.Password))
    {
        return await Task.FromResult(Results.Ok(DateTime.Now));
    }
    else
    {
        return await Task.FromResult(Results.BadRequest());
    }
});
app.MapGet("/Validate", async (HttpContext context,
    [FromHeader(Name = "Token")] string? token) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(context.Request.Headers));
    return await Task.FromResult(Results.Ok());
});
app.MapPost("/Refresh", async (HttpContext context, 
    [FromBody] DateTime? model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    if (model != null)
    {
        return await Task.FromResult(Results.Ok(DateTime.Now));
    }
    else
    {
        return await Task.FromResult(Results.BadRequest());
    }
});
app.MapPost("/SignOut", async (HttpContext context) =>
{
    return await Task.FromResult(Results.Ok());
});
#endregion

app.Run();
#endregion

#region Middlewares
class DefaultMiddleware
{
    private readonly RequestDelegate _dequestDelegate;
    private readonly ILogger<DefaultMiddleware> _logger;
    public DefaultMiddleware(RequestDelegate requestDelegate,
        ILogger<DefaultMiddleware> logger)
    {
        _dequestDelegate = requestDelegate;
        _logger = logger;
    }
    public async Task Invoke(HttpContext context)
    {
        _logger.LogInformation($"[{context.Request.Method}][{context.Request.Path}][{context.Request.QueryString}]");
        await _dequestDelegate(context);
    }
}
#endregion

#region Services
class Service
{
    private readonly ILogger<Service> _logger;
    public Service(ILogger<Service> logger)
    {
        _logger = logger;
    }
    public async Task Run() => await Task.Run(() => _logger.LogInformation("Run"));
}
#endregion

#region Dtos
class JsonDto
{
    public string? Text { get; set; }
    public int? Value { get; set; }
    public DateTime? Date { get; set; }
}
class SignInDto
{
    public string Account { get; set; } = null!;
    public string Password { get; set; } = null!;
}
class UploadDto
{
    public IFormFile File { get; set; } = null!;
    public string Name { get; set; } = null!;
}
#endregion