﻿using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BuildOverrideService.Http;

/// <summary>
///     Represents a methods info.
/// </summary>
internal class RestMethodInfo
{
    /// <summary>
    ///     Gets the route name for this method.
    /// </summary>
    public string RouteName
        => _route.Name;

    /// <summary>
    ///     Gets the HTTP method for this method.
    /// </summary>
    public string RouteMethod
        => _route.Method;

    private readonly Route _route;
    private readonly MethodInfo _info;
    private readonly Regex? _routeParamRegex;
    private readonly MatchEvaluator _routeParamEvaluator = new((a) => $@"(?<{a.Groups[1].Value}>[^\/]+?)");
    private readonly Dictionary<(int index, string name), Type> _parameters = new();
    private readonly Logger _log;
    private readonly IServiceProvider _provider;
    /// <summary>
    ///     Creates a new instance of <see cref="RestMethodInfo"/>.
    /// </summary>
    /// <param name="route">The route attribute instance.</param>
    /// <param name="info">The reflection method info.</param>
    public RestMethodInfo(Route route, MethodInfo info, IServiceProvider provider)
    {
        _log = Logger.GetLogger<RestMethodInfo>();

        _route = route;
        _info = info;

        if (!route.IsRegex)
            _routeParamRegex = ConstructRouteParamRegex(route.Name);

        var parameters = info.GetParameters();

        for (int i = 0; i != parameters.Length; i++)
        {
            var param = parameters[i];
            _parameters.Add((i, param.Name!), param.ParameterType);
        }
        
        _provider = provider;
    }

    /// <summary>
    ///     Determines if a provided route and method is a match to this method.
    /// </summary>
    /// <param name="route">The raw uri route.</param>
    /// <param name="method">The HTTP method.</param>
    public bool IsMatch(string route, string method)
    {
        if (_route.IsRegex && RouteMethod == method)
            return Regex.IsMatch(route, _route.Name);
        else return (_routeParamRegex?.IsMatch(route) ?? false) && method == RouteMethod;
    }

    /// <summary>
    ///     Gets all of the parameters' converted objects in the given route.
    /// </summary>
    /// <param name="route">The route attribute instance.</param>
    public object[]? GetConvertedParameters(string route)
    {
        object[] arr = new object[_parameters.Count];

        if (_route.IsRegex)
        {
            var regType = _parameters.FirstOrDefault();
            if (regType.Equals(default(KeyValuePair<string, Type>)))
                return new object[] { };

            var match = Regex.Match(route, _route.Name);

            if (regType.Value == typeof(MatchCollection))
                return new object[] { Regex.Matches(route, _route.Name) };
            else if (regType.Value == typeof(Match))
                return new object[] { match };
            else if (_parameters.Count == match.Groups.Count - 1)
            {
                // spead the groups out into the parameters
                for(int i = 0; i != arr.Length; i++)
                {
                    var t = _parameters.FirstOrDefault(x => x.Key.index == i);
                    var g = match.Groups[i + 1].Value;

                    arr[i] = Convert.ChangeType(g, t.Value);
                }

                return arr;
            }
            else return new object[] { };
        }


        var routeParams = GetRouteParams(route);

        foreach (var item in routeParams)
        {
            var rawParam = _parameters.FirstOrDefault(x => x.Key.name == item.Key);

            if (rawParam.Value == null)
                continue;

            try
            {
                arr[rawParam.Key.index] = Convert.ChangeType(item.Value, rawParam.Value);
            }
            catch (Exception x)
            {
                _log.Warn($"{x}", Severity.Rest);
                return null;
            }
        }

        return arr;
    }
    /// <summary>
    ///     Executes the route method as an an invokable asynchronous Task.
    /// </summary>
    /// <param name="instance">The route as an object instance.</param>
    /// <param name="parameters">The argument signature of the given route object.</param>
    /// <returns>The RestResult of this method.</returns>
    public async Task<RestResult> ExecuteAsync(RestModuleBase? instance, params object[] parameters)
        => await((Task<RestResult>?)_info.Invoke(instance, parameters)!).ConfigureAwait(false);

    private Regex ConstructRouteParamRegex(string route)
    {
        var s = Regex.Escape(route.Replace("{", "C_LBR").Replace("}", "C_RBR")).Replace("C_LBR", "{").Replace("C_RBR", "}");
        var val = Regex.Replace(s, @"{(.+?)}", _routeParamEvaluator);
        return new Regex($"^{val}(?>/|)$".Replace("/", "\\/"));
    }

    /// <summary>
    ///     Gets the key and value of parameters in the given route.
    /// </summary>
    /// <param name="route">The route attribute instance.</param>
    private Dictionary<string, string> GetRouteParams(string route)
    {
        if (_routeParamRegex == null)
            return new();

        var matches = _routeParamRegex.Matches(route);

        var dict = new Dictionary<string, string>();

        foreach (var item in _parameters)
        {
            var match = matches.FirstOrDefault(x => x.Groups.ContainsKey(item.Key.name));

            if (match == null)
                continue;

            dict.Add(item.Key.name, match.Groups[item.Key.name].Value);
        }

        return dict;
    }
}
