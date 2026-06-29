using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Infrastructure.Services;

public class BackgroundTaskService : BackgroundService
{
    private readonly Channel<Func<IServiceScopeFactory, CancellationToken, Task>> _channel =
        Channel.CreateBounded<Func<IServiceScopeFactory, CancellationToken, Task>>(100);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundTaskService> _logger;

    public BackgroundTaskService(IServiceScopeFactory scopeFactory, ILogger<BackgroundTaskService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<string> EnqueueAsync(Func<IServiceScopeFactory, CancellationToken, Task> taskFunc)
    {
        await _channel.Writer.WriteAsync(taskFunc);
        return "queued";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var taskFunc in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await taskFunc(_scopeFactory, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "后台任务执行失败");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 应用关闭时的正常取消，不视为异常
        }
    }
}