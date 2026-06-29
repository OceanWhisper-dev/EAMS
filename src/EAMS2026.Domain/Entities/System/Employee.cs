namespace EAMS2026.Domain.Entities.System;

public class Employee : BaseEntity
{
    public string EmployeeNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public long? DepartmentId { get; set; }
    public string? Position { get; set; }
    public DateOnly? HireDate { get; set; }
    public DateOnly? ResignationDate { get; set; }
    public bool Status { get; set; } = true;
    public string? DepartmentName { get; set; }

    public Department? Department { get; set; }
}