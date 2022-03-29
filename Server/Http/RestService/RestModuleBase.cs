
using BuildOverrideService.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;

namespace BuildOverrideService.Http;

public class RestModuleBase
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public HttpListenerContext Context { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public HttpServer? RestServer { get; private set; }

    public HttpListenerRequest Request
        => Context.Request;
    public HttpListenerResponse Response
        => Context.Response;
    public JsonSerializer Serializer
        => RestServer!.Provider.GetRequiredService<JsonSerializer>();

    public OverrideService OverrideService
        => RestServer!.Provider.GetRequiredService<OverrideService>();

    public DatabaseService DatabaseSerivce
        => RestServer!.Provider.GetRequiredService<DatabaseService>();

    public DependencyService DependencyService
        => RestServer!.Provider.GetRequiredService<DependencyService>();

    internal RestModuleInfo? ModuleInfo { get; private set; }

    internal void SetRefreshCookie(string token)
    {
#if DEBUG == false
        Response.AppendHeader("Set-Cookie", $"r_={token}; Path=/; Max-Age={60 * 60 * 24 * 7}; HttpOnly; Domain=ddocs.io; Secure;");
#else
        Response.AppendHeader("Set-Cookie", $"r_={token}; Path=/; Max-Age={60 * 60 * 24 * 7}; HttpOnly;");
#endif
    }

    internal void ClearRefreshCookie()
    {
#if DEBUG == false
        Response.AppendHeader("Set-Cookie", $"r_=none; Path=/; Max-Age=0; HttpOnly; Domain=ddocs.io; Secure;");
#else
        Response.AppendHeader("Set-Cookie", $"r_=none; Path=/; Max-Age=0; HttpOnly;");
#endif
    }

    internal RestModuleBase InitializeModule(HttpListenerContext context, RestModuleInfo info, HttpServer server)
    {
        this.Context = context;
        this.ModuleInfo = info;
        this.RestServer = server;
        return this;
    }

    protected TReturn? GetBody<TReturn>() where TReturn : class
    {
        if (!Request.HasEntityBody)
            return null;

        try
        {
            using(var sr = new StreamReader(Request.InputStream))
            using(var reader = new JsonTextReader(sr))
            {
                return Serializer.Deserialize<TReturn>(reader);
            }
        }
        catch(Exception x)
        {
            Logger.GetLogger<RestModuleBase>().Warn("Failed to read body: ", exception: x);
            return null;
        }
    }

    public override bool Equals(object? obj)
    {
        try
        {
            if (obj == null)
                return false;

            if (obj is RestModuleBase other)
            {
                return other.ModuleInfo?.Equals(this.ModuleInfo) ?? false;
            }
            else return base.Equals(obj);
        }
        catch
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
