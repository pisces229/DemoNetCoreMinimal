using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Web;

#region WebApplicationBuilder
var builder = WebApplication.CreateBuilder(args);

builder.Logging
    //.SetMinimumLevel(LogLevel.Information)
    .AddFilter("DemoNetCoreMinimal", LogLevel.Trace)
    .AddFilter("Microsoft", LogLevel.Information)
    .AddFilter("System", LogLevel.Information)
    .AddConsole();

builder.Services.AddCors();

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
//認證中介軟體
//app.UseAuthentication()
//授權中介軟體
//app.UseAuthorization()
//跨域中介軟體
//app.UseCors()
//全域性異常處理中介軟體
//app.UseExceptionHandler()
//代理頭資訊轉發中介軟體
//app.UseForwardedHeaders()
//Https重定向中介軟體
//app.UseHttpsRedirection()
//特殊響應頭的安全增強中介軟體
//app.UseHsts()
//HTTP請求和響應日誌中介軟體
//app.UseHttpLogging()
//輸出快取中介軟體
//app.UseResponseCaching()
//響應壓縮中介軟體
//app.UseResponseCompression()
//Session中介軟體
//app.UseSession()
//靜態檔案中介軟體
//app.UseStaticFiles()
//app.UseFileServer()
//WebSocket支援中介軟體
//app.UseWebSockets()
#endregion

app.UseCors(config => config.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithExposedHeaders("content-disposition"));
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
app.MapGet("/free", async (HttpContext context) =>
{
    //context.Response.Headers.Add("Cache-Control", "max-age=6");
    //return await Task.FromResult(Results.Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
    return await Task.FromResult(Results.Text("Free Success"));
});
app.MapGet("/auth", async (HttpContext context) =>
{
    return await Task.FromResult(Results.Text("Auth Success"));
});
app.MapGet("/router", async (HttpContext context) =>
{
    var result = true;
    return await Task.FromResult(result);
});
app.MapGet("/valueFromQuery", async (HttpContext context,
    [FromQuery(Name = "model")] string model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    return await Task.FromResult(Results.Text(model));
});
app.MapPost("/valueFromBody", async (HttpContext context, 
    [FromBody] string model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    return await Task.FromResult(Results.Text(model));
});
app.MapGet("/jsonFromQuery", async  (HttpContext context, 
    [FromQuery(Name = "Text")] string? text, 
    [FromQuery(Name = "Value")] int? value, 
    [FromQuery(Name = "Date")] DateTime? date) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(text));
    app.Logger.LogInformation(JsonSerializer.Serialize(value));
    app.Logger.LogInformation(JsonSerializer.Serialize(date));
    return await Task.FromResult(Results.Json(new { text, value, date }));
});
app.MapPost("/jsonFromBody", async (HttpContext context, 
    [FromBody] JsonDto model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    return await Task.FromResult(Results.Json(model));
});
app.MapPost("/upload", async (HttpContext context) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(context.Request.Form.Files));
    app.Logger.LogInformation(JsonSerializer.Serialize(context.Request.Form.ToList()));
    return await Task.FromResult(Results.Text(""));
});
app.MapGet("/download", async (HttpContext context) =>
{
    var file = await Task.FromResult(new FileInfo("c:/workspace/demoNetCoreMinimal/download.zip"));
    //return Results.BadRequest("BadRequest");
    if (file.Exists)
    {
        //context.Response.StatusCode = 200;
        context.Response.ContentType = "application/octet-stream";
        context.Response.Headers.Add("content-disposition", $"attachment; filename={HttpUtility.UrlEncode(file.Name)}");
        //await context.Response.SendFileAsync(file.FullName);
        var buffer = new byte[16 * 1024];
        using (var fileStream = file.OpenRead())
        {
            var read = 0;
            while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                await context.Response.Body.WriteAsync(buffer, 0, read);
            }
        }
        //result.Data!.FileInfo.Delete();
    }
    else
    {
        //context.Response.StatusCode = 200;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("File Not Exists");
    }
});
// login
app.MapPost("/signIn", async (HttpContext context, 
    [FromBody] SignInDto model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    if (!string.IsNullOrEmpty(model.Account) && !string.IsNullOrEmpty(model.Password))
    {
        return await Task.FromResult(Results.Text(DateTime.Now.AddSeconds(10).Ticks.ToString()));
    }
    else
    {
        return await Task.FromResult(Results.BadRequest());
    }
});
app.MapGet("/validate", async (HttpContext context,
    [FromHeader(Name = "Token")] string? token) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(context.Request.Headers));
    return await Task.FromResult(Results.Ok());
});
app.MapPost("/refresh", async (HttpContext context, 
    [FromBody] string model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    if (model != null)
    {
        return await Task.FromResult(Results.Text(DateTime.Now.AddSeconds(10).Ticks.ToString()));
    }
    else
    {
        return await Task.FromResult(Results.BadRequest());
    }
});
app.MapPost("/signOut", async (HttpContext context) =>
{
    return await Task.FromResult(Results.Text(""));
});
#endregion

app.Run();
#endregion

#region Middlewares
class DefaultMiddleware
{
    private readonly RequestDelegate _dequestDelegate;
    private readonly ILogger<DefaultMiddleware> _logger;
    private readonly List<string> _authorizationPath = new List<string>()
    {
        "/auth",
        //"/valueFromQuery",
        //"/valueFromBody",
        //"/jsonFromQuery",
        //"/jsonFromBody",
        //"/download",
        //"/upload",
    };
    public DefaultMiddleware(RequestDelegate requestDelegate,
        ILogger<DefaultMiddleware> logger)
    {
        _dequestDelegate = requestDelegate;
        _logger = logger;
    }
    public async Task Invoke(HttpContext context)
    {
        _logger.LogInformation($"[{context.Request.Method}][{context.Request.Path}][{context.Request.QueryString}]");
        //await _dequestDelegate(context);
        if (!_authorizationPath.Contains(context.Request.Path))
        {
            await _dequestDelegate(context);
        }
        else
        {
            if (context.Request.Headers.Keys.Contains("token"))
            {
                try
                {
                    var token = new DateTime(Convert.ToInt64(context.Request.Headers["token"]));
                    if (token > DateTime.Now)
                    {
                        await _dequestDelegate(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                    context.Response.StatusCode = 403;
                }
            }
            else
            {
                context.Response.StatusCode = 403;
            }
        }
    }
}
class TokenMiddleware
{
    private readonly RequestDelegate _dequestDelegate;
    public TokenMiddleware(RequestDelegate requestDelegate)
    {
        _dequestDelegate = requestDelegate;
    }
    public async Task Invoke(HttpContext context)
    {
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