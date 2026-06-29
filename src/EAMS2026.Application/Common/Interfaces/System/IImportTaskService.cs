using EAMS2026.Application.Services.System;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IImportTaskService
{
    string CreateTask();
    void UpdateProgress(string taskId, int progress, string message = "");
    void Complete(string taskId, string message = "");
    void Fail(string taskId, string message);
    ImportTaskInfo? GetTask(string taskId);
    void Cleanup(TimeSpan olderThan);
}