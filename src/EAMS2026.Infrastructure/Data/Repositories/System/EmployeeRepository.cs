using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public EmployeeRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(Employee entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO sys_employees (employee_no, name, gender, phone, email, department_id, position, hire_date, resignation_date, status, is_deleted, created_at, created_by, updated_at, updated_by)
                    VALUES (@EmployeeNo, @Name, @Gender, @Phone, @Email, @DepartmentId, @Position, @HireDate, @ResignationDate, @Status, FALSE, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await SoftDeleteAsync(id);
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT e.*, d.name AS department_name
                    FROM sys_employees e
                    LEFT JOIN sys_departments d ON e.department_id = d.id
                    WHERE e.is_deleted = FALSE
                    ORDER BY e.employee_no";
        return await conn.QueryAsync<Employee>(sql);
    }

    public async Task<Employee?> GetByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT e.*, d.name AS department_name
                    FROM sys_employees e
                    LEFT JOIN sys_departments d ON e.department_id = d.id
                    WHERE e.id = @Id AND e.is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<Employee>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Employee>> GetByDepartmentIdAsync(long departmentId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT e.*, d.name AS department_name
                    FROM sys_employees e
                    LEFT JOIN sys_departments d ON e.department_id = d.id
                    WHERE e.department_id = @DepartmentId AND e.is_deleted = FALSE
                    ORDER BY e.employee_no";
        return await conn.QueryAsync<Employee>(sql, new { DepartmentId = departmentId });
    }

    public async Task<Employee?> GetByEmployeeNoAsync(string employeeNo)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_employees WHERE employee_no = @EmployeeNo AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeNo = employeeNo });
    }

    public async Task<bool> IsEmployeeNoExistsAsync(string employeeNo, long? excludeId = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (excludeId.HasValue)
        {
            var sql = "SELECT COUNT(1) FROM sys_employees WHERE employee_no = @EmployeeNo AND is_deleted = FALSE AND id != @ExcludeId";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { EmployeeNo = employeeNo, ExcludeId = excludeId.Value });
            return count > 0;
        }
        else
        {
            var sql = "SELECT COUNT(1) FROM sys_employees WHERE employee_no = @EmployeeNo AND is_deleted = FALSE";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { EmployeeNo = employeeNo });
            return count > 0;
        }
    }

    public async Task<bool> SoftDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_employees SET is_deleted = TRUE, updated_at = NOW() WHERE id = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<bool> UpdateAsync(Employee entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE sys_employees SET employee_no = @EmployeeNo, name = @Name, gender = @Gender,
                    phone = @Phone, email = @Email, department_id = @DepartmentId, position = @Position,
                    hire_date = @HireDate, resignation_date = @ResignationDate, status = @Status, updated_at = NOW(), updated_by = @UpdatedBy
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<IEnumerable<Employee>> GetDeletedAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT e.*, d.name AS department_name
                    FROM sys_employees e
                    LEFT JOIN sys_departments d ON e.department_id = d.id
                    WHERE e.is_deleted = TRUE
                    ORDER BY e.updated_at DESC";
        return await conn.QueryAsync<Employee>(sql);
    }

    public async Task<bool> HardDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM sys_employees WHERE id = @Id", new { Id = id }) > 0;
    }
}