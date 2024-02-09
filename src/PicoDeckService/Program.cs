using PicoDeckService;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(config =>
    {
        config.ServiceName = "PicoDeck Service";
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<BackgroundServiceManager>();
        services.AddHostedService<PicoDeckServer>();
        services.AddHostedService<PicoDeckTaskbarListener>();
    })
    .Build();

host.Run();