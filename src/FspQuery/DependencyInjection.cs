using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace FspQuery;
public static class DependencyInjection
{
    public static IServiceCollection AddFspQuery(this IServiceCollection services)
        => AddFspQuery(services, default);

    public static IServiceCollection AddFspQuery(this IServiceCollection services, JsonSerializerOptions? options)
    {
        services
            .AddSingleton<IObjectIndexer, ObjectIndexer>(serviceProvider => options == default ? new() : new(options))
            .AddSingleton<IFspQueryLogic, FspQueryLogic>()
            .AddSingleton<IFspQueryValidator, FspQueryValidator>();

        return services;
    }
}
