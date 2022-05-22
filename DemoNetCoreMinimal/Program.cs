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

#region �����n��
//�{�Ҥ����n��
//app.UseAuthentication()
//���v�����n��
//app.UseAuthorization()
//��줤���n��
//app.UseCors()
//����ʲ��`�B�z�����n��
//app.UseExceptionHandler()
//�N�z�Y��T��o�����n��
//app.UseForwardedHeaders()
//Https���w�V�����n��
//app.UseHttpsRedirection()
//�S���T���Y���w���W�j�����n��
//app.UseHsts()
//HTTP�ШD�M�T����x�����n��
//app.UseHttpLogging()
//��X�֨������n��
//app.UseResponseCaching()
//�T�����Y�����n��
//app.UseResponseCompression()
//Session�����n��
//app.UseSession()
//�R�A�ɮפ����n��
//app.UseStaticFiles()
//app.UseFileServer()
//WebSocket�䴩�����n��
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
    [FromQuery(Name = "Text")] string? text, 
    [FromQuery(Name = "Value")] int? value, 
    [FromQuery(Name = "Date")] DateTime? date) =>
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
        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/download";
        context.Response.Headers.Add("content-disposition", $"attachment; filename=Download.zip");
        await context.Response.SendFileAsync(source);
    }
    else
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("File Not Exists");
    }
});
// login
app.MapPost("/SignIn", async (HttpContext context, 
    [FromBody] SignInDto model) =>
{
    app.Logger.LogInformation(JsonSerializer.Serialize(model));
    if (!string.IsNullOrEmpty(model.Account) && !string.IsNullOrEmpty(model.Password))
    {
        return await Task.FromResult(Results.Ok(DateTime.Now.AddSeconds(10)));
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
        return await Task.FromResult(Results.Ok(DateTime.Now.AddSeconds(10)));
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
    //private readonly List<string> _authorizationPath = new List<string>()
    //{
    //    "/ValueFromQuery",
    //    "/ValueFromBody",
    //    "/JsonFromQuery",
    //    "/JsonFromBody",
    //    "/Download",
    //    "/Upload",
    //};
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
        //if (!_authorizationPath.Contains(context.Request.Path))
        //{
        //    await _dequestDelegate(context);
        //}
        //else
        //{
        //    if (context.Request.Headers.Keys.Contains("token"))
        //    {
        //        try
        //        {
        //            var token = Convert.ToDateTime(context.Request.Headers["token"]);
        //            if (token > DateTime.Now)
        //            {
        //                await _dequestDelegate(context);
        //            }
        //            else
        //            {
        //                context.Response.StatusCode = 401;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            _logger.LogError(e.ToString());
        //            context.Response.StatusCode = 403;
        //        }
        //    }
        //    else
        //    {
        //        context.Response.StatusCode = 403;
        //    }
        //}
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