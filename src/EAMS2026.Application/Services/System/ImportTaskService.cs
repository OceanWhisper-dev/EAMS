using System.Collections.Concurrent;
using EAMS2026.Application.Common.Interfaces.System;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.System;

public class ImportTaskInfo
{
    public string TaskId { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string Status { get; set; } = "pending";
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ImportTaskService : IImportTaskService
{
    private readonly ConcurrentDictionary<string, ImportTaskInfo> _tasks = new();
    private readonly ILogger<ImportTaskService> _logger;

    public ImportTaskService(ILogger<ImportTaskService> logger)
    {
        _logger = logger;
    }

    public string CreateTask()
    {
        var taskId = Guid.NewGuid().ToString("N");
        _tasks[taskId] = new ImportTaskInfo
        {
            TaskId = taskId,
            Status = "running",
            CreatedAt = DateTime.Now
        };
        return taskId;
    }

    public void UpdateProgress(string taskId, int progress, string message = "")
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            task.Progress = progress;
            task.Message = message;
        }
    }

    public void Complete(string taskId, string message = "")
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            task.Progress = 100;
            task.Status = "completed";
            task.Message = message;
        }
    }

    public void Fail(string taskId, string message)
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            task.Status = "failed";
            task.Message = message;
        }
    }

    public ImportTaskInfo? GetTask(string taskId)
    {
        _tasks.TryGetValue(taskId, out var task);
        return task;
    }

    public void Cleanup(TimeSpan olderThan)
    {
        var cutoff = DateTime.Now - olderThan;
        foreach (var kvp in _tasks)
        {
            if (kvp.Value.CreatedAt < cutoff)
                _tasks.TryRemove(kvp.Key, out _);
        }
    }
}