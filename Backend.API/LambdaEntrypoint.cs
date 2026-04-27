using Amazon.Lambda.AspNetCoreServer;

namespace Backend;

public class LambdaEntryPoint : APIGatewayProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder.UseStartup<Program>();
    }

    protected override void Init(IHostBuilder builder)
    {
    }
}