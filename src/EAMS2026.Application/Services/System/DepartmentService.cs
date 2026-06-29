using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.System;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IOperationLogRepository _logRepository;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(IDepartmentRepository departmentRepository, IOperationLogRepository logRepository, ILogger<DepartmentService> logger)
    {
        _departmentRepository = departmentRepository;
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Department>> GetTreeAsync()
    {
        return await _departmentRepository.GetTreeAsync();
    }

    public async Task<IEnumerable<Department>> GetAllAsync()
    {
        return await _departmentRepository.GetAllAsync();
    }

    public async Task<Department?> GetByIdAsync(long id)
    {
        return await _departmentRepository.GetByIdAsync(id);
    }

    public async Task<Department?> GetByCodeAsync(string code)
    {
        return await _departmentRepository.GetByCodeAsync(code);
    }

    public async Task<bool> IsCodeExistsAsync(string code)
    {
        return await _departmentRepository.IsCodeExistsAsync(code);
    }

    public async Task<(bool Success, string Message)> CreateAsync(Department department, long operatorId)
    {
        if (await _departmentRepository.IsCodeExistsAsync(department.Code))
            return (false, "部门编码已存在");

        department.CreatedBy = operatorId;
        department.UpdatedBy = operatorId;
        var id = await _departmentRepository.AddAsync(department);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Create",
            Module = "Department",
            EntityType = "Department",
            EntityId = id,
            Description = $"创建部门: {department.Name}"
        });

        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Department department, long operatorId)
    {
        var existing = await _departmentRepository.GetByIdAsync(department.Id);
        if (existing == null)
            return (false, "部门不存在");

        if (await _departmentRepository.IsCodeExistsAsync(department.Code, department.Id))
            return (false, "部门编码已存在");

        department.UpdatedBy = operatorId;
        await _departmentRepository.UpdateAsync(department);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Update",
            Module = "Department",
            EntityType = "Department",
            EntityId = department.Id,
            Description = $"更新部门: {department.Name}"
        });

        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(long id, long operatorId)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
            return (false, "部门不存在");

        if (id == 1)
            return (false, "不能删除根部门");

        if (await _departmentRepository.HasChildrenAsync(id))
            return (false, "该部门下有子部门，无法删除");

        await _departmentRepository.SoftDeleteAsync(id);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Delete",
            Module = "Department",
            EntityType = "Department",
            EntityId = id,
            Description = $"删除部门: {department.Name}"
        });

        return (true, "删除成功");
    }
}