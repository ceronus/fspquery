using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace FspQuery;
public static class DependencyInjection
{
    public static IServiceCollection AddFspQuery(this IServiceCollection services)
        => AddFspQuery(services);

    [Obsolete]
    public static IServiceCollection AddFspQuery(this IServiceCollection services, JsonSerializerOptions? options = default)
        => AddFspQuery(services, true, JsonKnownNamingPolicy.Unspecified, options);

    public static IServiceCollection AddFspQuery(
        this IServiceCollection services,
        bool? useCaseInsensitive = true,
        JsonKnownNamingPolicy? namingPolicy = JsonKnownNamingPolicy.Unspecified,
        JsonSerializerOptions? options = default)
    {
        services
            .AddSingleton<IObjectIndexer, ObjectIndexer>(ObjectIndexer.ImplemtationFactory(useCaseInsensitive, namingPolicy, options))
            .AddSingleton<IFspQueryLogic, FspQueryLogic>()
            .AddSingleton<IFspQueryValidator, FspQueryValidator>();

        return services;
    }
}
