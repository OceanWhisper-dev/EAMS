namespace EAMS2026.Domain.DTOs.System;

public class RoleUserDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public long? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public bool Status { get; set; } = true;
}