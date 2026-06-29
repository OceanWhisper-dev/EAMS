using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EAMS2026.Application.Services.System;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOperationLogRepository _logRepository;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IEmployeeRepository employeeRepository, IOperationLogRepository logRepository, ILogger<EmployeeService> logger)
    {
        _employeeRepository = employeeRepository;
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        return await _employeeRepository.GetAllAsync();
    }

    public async Task<Employee?> GetByIdAsync(long id)
    {
        return await _employeeRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Employee>> GetByDepartmentIdAsync(long departmentId)
    {
        return await _employeeRepository.GetByDepartmentIdAsync(departmentId);
    }

    public async Task<Employee?> GetByEmployeeNoAsync(string employeeNo)
    {
        return await _employeeRepository.GetByEmployeeNoAsync(employeeNo);
    }

    public async Task<(bool Success, string Message)> CreateAsync(Employee employee, long operatorId)
    {
        if (await _employeeRepository.IsEmployeeNoExistsAsync(employee.EmployeeNo))
            return (false, "工号已存在");

        employee.CreatedBy = operatorId;
        employee.UpdatedBy = operatorId;
        var id = await _employeeRepository.AddAsync(employee);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Create",
            Module = "Employee",
            EntityType = "Employee",
            EntityId = id,
            Description = $"创建员工: {employee.Name}",
            NewValue = JsonSerializer.Serialize(employee)
        });

        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Employee employee, long operatorId)
    {
        var existing = await _employeeRepository.GetByIdAsync(employee.Id);
        if (existing == null)
            return (false, "员工不存在");

        if (await _employeeRepository.IsEmployeeNoExistsAsync(employee.EmployeeNo, employee.Id))
            return (false, "工号已存在");

        employee.UpdatedBy = operatorId;
        await _employeeRepository.UpdateAsync(employee);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Update",
            Module = "Employee",
            EntityType = "Employee",
            EntityId = employee.Id,
            Description = $"更新员工: {employee.Name}",
            OldValue = JsonSerializer.Serialize(existing),
            NewValue = JsonSerializer.Serialize(employee)
        });

        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(long id, long operatorId)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null)
            return (false, "员工不存在");

        await _employeeRepository.SoftDeleteAsync(id);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Delete",
            Module = "Employee",
            EntityType = "Employee",
            EntityId = id,
            Description = $"删除员工: {employee.Name}"
        });

        return (true, "删除成功");
    }

    public async Task<IEnumerable<Employee>> GetDeletedAsync()
    {
        return await _employeeRepository.GetDeletedAsync();
    }

    public async Task<(bool Success, string Message)> HardDeleteAsync(long id, long operatorId)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null)
        {
            var deleted = await _employeeRepository.GetDeletedAsync();
            employee = deleted.FirstOrDefault(e => e.Id == id);
            if (employee == null)
                return (false, "员工不存在");
        }

        await _employeeRepository.HardDeleteAsync(id);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "HardDelete",
            Module = "Employee",
            EntityType = "Employee",
            EntityId = id,
            Description = $"永久删除员工: {employee.Name}"
        });

        return (true, "已永久删除");
    }
}