using EAMS2026.Application.Services.System;
using EAMS2026.Domain.DTOs.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Entities.Attendance;
using EAMS2026.Domain.Enums;
using EAMS2026.Domain.Interfaces.Repositories.Attendance;
using EAMS2026.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text.Json;

namespace EAMS2026.Api.Controllers.System;

[Authorize]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(500)]
[Route("api/import-export")]
public class ImportExportController : BaseController
{
    private readonly ExcelService _excelService;
    private readonly DepartmentService _departmentService;
    private readonly EmployeeService _employeeService;
    private readonly UserService _userService;
    private readonly RoleService _roleService;
    private readonly PermissionService _permissionService;
    private readonly IAttendanceRepository _attendanceRepository;

    public ImportExportController(
        ExcelService excelService,
        DepartmentService departmentService,
        EmployeeService employeeService,
        UserService userService,
        RoleService roleService,
        PermissionService permissionService,
        IAttendanceRepository attendanceRepository)
    {
        _excelService = excelService;
        _departmentService = departmentService;
        _employeeService = employeeService;
        _userService = userService;
        _roleService = roleService;
        _permissionService = permissionService;
        _attendanceRepository = attendanceRepository;
    }

    [HttpGet("export/{module}")]
    public async Task<IActionResult> Export(string module)
    {
        byte[] excelBytes;
        string fileName;

        switch (module.ToLower())
        {
            case "department":
                {
                    var data = await _departmentService.GetAllAsync();
                    var table = BuildDataTable(data,
                        ("ID", d => d.Id),
                        ("部门名称", d => d.Name),
                        ("部门编码", d => d.Code),
                        ("上级部门ID", d => d.ParentId),
                        ("排序", d => d.SortOrder),
                        ("状态", d => d.Status)
                    );
                    excelBytes = _excelService.GenerateExcel(table, "部门数据");
                    fileName = $"部门数据_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            case "employee":
                {
                    var data = await _employeeService.GetAllAsync();
                    var departments = await _departmentService.GetAllAsync();
                    var deptDict = departments.ToDictionary(d => d.Id, d => d.Code);
                    var table = BuildDataTable(data,
                        ("ID", e => e.Id),
                        ("工号", e => e.EmployeeNo),
                        ("姓名", e => e.Name),
                        ("性别", e => e.Gender),
                        ("电话", e => e.Phone),
                        ("邮箱", e => e.Email),
                        ("部门编码", e => e.DepartmentId.HasValue && deptDict.TryGetValue(e.DepartmentId.Value, out var code) ? code : ""),
                        ("部门ID", e => e.DepartmentId),
                        ("职位", e => e.Position),
                        ("入职日期", e => e.HireDate),
                        ("状态", e => e.Status)
                    );
                    excelBytes = _excelService.GenerateExcel(table, "员工数据");
                    fileName = $"员工数据_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            case "user":
                {
                    var data = await _userService.GetAllAsync();
                    var table = BuildDataTable(data,
                        ("ID", u => u.Id),
                        ("用户名", u => u.Username),
                        ("姓名", u => !string.IsNullOrEmpty(u.EmployeeName) ? u.EmployeeName : u.Username),
                        ("员工ID", u => u.EmployeeId),
                        ("状态", u => u.Status),
                        ("最后登录时间", u => u.LastLoginAt)
                    );
                    excelBytes = _excelService.GenerateExcel(table, "用户数据");
                    fileName = $"用户数据_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            case "role":
                {
                    var data = await _roleService.GetAllAsync();
                    var table = BuildDataTable(data,
                        ("ID", r => r.Id),
                        ("角色名称", r => r.Name),
                        ("角色编码", r => r.Code),
                        ("描述", r => r.Description),
                        ("状态", r => r.Status)
                    );
                    excelBytes = _excelService.GenerateExcel(table, "角色数据");
                    fileName = $"角色数据_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            case "permission":
                {
                    var data = await _permissionService.GetTreeAsync();
                    var flatData = FlattenTree(data);
                    var table = BuildDataTable(flatData,
                        ("ID", p => p.Id),
                        ("权限名称", p => p.Name),
                        ("权限编码", p => p.Code),
                        ("类型", p => p.Type),
                        ("上级权限ID", p => p.ParentId),
                        ("路由路径", p => p.Path),
                        ("排序", p => p.SortOrder)
                    );
                    excelBytes = _excelService.GenerateExcel(table, "权限数据");
                    fileName = $"权限数据_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            case "day-type":
                {
                    var data = await _attendanceRepository.GetDayTypesAsync();
                    var table = BuildDataTable(data,
                        ("ID", d => d.Id),
                        ("类型名称", d => d.DayTypeName),
                        ("说明", d => d.DayTypeCaption)
                    );
                    excelBytes = _excelService.GenerateExcel(table, "考勤类型");
                    fileName = $"考勤类型_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            case "scheme-class":
                {
                    var data = await _attendanceRepository.GetSchemeClassesAsync();
                    var table = BuildDataTable(data,
                        ("ID", s => s.Id),
                        ("类别名称", s => s.ClassName),
                        ("周期数", s => s.Periods),
                        ("班次数", s => s.ClassPeriods),
                        ("描述", s => s.ClassDescription)
                    );
                    excelBytes = _excelService.GenerateExcel(table, "排班类别");
                    fileName = $"排班类别_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            case "plan-time":
                {
                    var data = await _attendanceRepository.GetPlanTimesAsync();
                    var table = BuildDataTable(data,
                        ("ID", p => p.Id),
                        ("计划名称", p => p.PlanName),
                        ("日期类型ID", p => p.DayTypeId),
                        ("描述", p => p.Description),
                        ("上班时间", p => p.BTime),
                        ("下班时间", p => p.ETime)
                    );
                    excelBytes = _excelService.GenerateExcel(table, "计划标准时间");
                    fileName = $"计划标准时间_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            case "holiday":
                {
                    var data = await _attendanceRepository.GetHolidaysAsync(null);
                    var table = BuildDataTable(data,
                        ("ID", h => h.Id),
                        ("年份", h => h.IYear),
                        ("日期", h => h.SDate.ToString("yyyy-MM-dd")),
                        ("假日名称", h => h.SName),
                        ("开始时间", h => h.BTime?.ToString(@"hh\:mm\:ss") ?? ""),
                        ("结束时间", h => h.ETime?.ToString(@"hh\:mm\:ss") ?? ""),
                        ("描述", h => h.SDescription)
                    );
                    excelBytes = _excelService.GenerateExcel(table, "节假日");
                    fileName = $"节假日_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            case "fee-calculator":
                {
                    var data = await _attendanceRepository.GetFeeCalculatorsAsync(null);
                    var table = BuildDataTable(data,
                        ("ID", f => f.Id),
                        ("日期类型ID", f => f.DayTypeId),
                        ("范围A(分钟)", f => f.RangeA),
                        ("范围B(分钟)", f => f.RangeB),
                        ("金额(元)", f => f.RangePrice)
                    );
                    excelBytes = _excelService.GenerateExcel(table, "费用计算规则");
                    fileName = $"费用计算规则_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            case "employee-class-ref":
                {
                    var data = await _attendanceRepository.GetEmployeeRefSchemeClassesAsync(null);
                    var table = BuildDataTable(data,
                        ("ID", e => e.Id),
                        ("员工ID", e => e.EmployeeId),
                        ("班次ID", e => e.ClassId),
                        ("周期序号", e => e.PeriodNo),
                        ("生效日期", e => e.EffDate.ToString("yyyy-MM-dd")),
                        ("失效日期", e => e.ExpDate?.ToString("yyyy-MM-dd") ?? "")
                    );
                    excelBytes = _excelService.GenerateExcel(table, "员工关联班次");
                    fileName = $"员工关联班次_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                break;

            default:
                return Fail($"不支持的导出模块: {module}");
        }

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("template/{module}")]
    public IActionResult DownloadTemplate(string module)
    {
        string[] columns;
        string fileName;

        switch (module.ToLower())
        {
            case "department":
                columns = ["部门名称", "部门编码", "上级部门ID", "排序", "状态"];
                fileName = $"部门导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            case "employee":
                columns = ["工号", "姓名", "性别", "电话", "邮箱", "部门编码", "职位", "入职日期", "状态"];
                fileName = $"员工导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            case "user":
                columns = ["用户名", "密码", "姓名", "员工工号", "状态"];
                fileName = $"用户导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            case "role":
                columns = ["角色名称", "角色编码", "描述", "状态"];
                fileName = $"角色导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            case "permission":
                columns = ["权限名称", "权限编码", "类型", "上级权限编码", "路由路径", "排序"];
                fileName = $"权限导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            case "day-type":
                columns = ["类型名称", "说明"];
                fileName = $"考勤类型导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            case "scheme-class":
                columns = ["类别名称", "周期数", "班次数", "描述"];
                fileName = $"排班类别导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            case "plan-time":
                columns = ["计划名称", "日期类型ID", "描述", "上班时间", "下班时间"];
                fileName = $"计划标准时间导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            case "holiday":
                columns = ["年份", "日期", "假日名称", "开始时间", "结束时间", "描述"];
                fileName = $"节假日导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            case "fee-calculator":
                columns = ["日期类型ID", "范围A(分钟)", "范围B(分钟)", "金额(元)"];
                fileName = $"费用计算规则导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            case "employee-class-ref":
                columns = ["员工ID", "班次ID", "周期序号", "生效日期", "失效日期"];
                fileName = $"员工关联班次导入模板_{DateTime.Now:yyyyMMdd}.xlsx";
                break;

            default:
                return Fail($"不支持的导入模块: {module}");
        }

        var bytes = _excelService.CreateTemplate(columns, "导入模板");
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("import/{module}")]
    public async Task<IActionResult> Import(string module, IFormFile file, [FromQuery] bool overwrite = false)
    {
        if (file == null || file.Length == 0)
            return Fail("请选择要导入的文件");

        byte[] fileBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            fileBytes = ms.ToArray();
        }

        var dataTable = _excelService.ReadExcel(fileBytes);

        switch (module.ToLower())
        {
            case "department":
                return await ImportDepartments(dataTable, overwrite);

            case "employee":
                return await ImportEmployees(dataTable, overwrite);

            case "user":
                return await ImportUsers(dataTable, overwrite);

            case "role":
                return await ImportRoles(dataTable, overwrite);

            case "permission":
                return await ImportPermissions(dataTable, overwrite);

            case "day-type":
            case "scheme-class":
            case "plan-time":
            case "holiday":
            case "fee-calculator":
            case "employee-class-ref":
                return await ImportAttendanceConfig(module, dataTable, overwrite);

            default:
                return Fail($"不支持的导入模块: {module}");
        }
    }

    private async Task<IActionResult> ImportDepartments(DataTable table, bool overwrite = false)
    {
        var successCount = 0;
        var failCount = 0;
        var duplicateCount = 0;
        var errors = new List<string>();

        // Build a mapping from old ID (from export) to department code
        var oldIdToCode = new Dictionary<string, string>();
        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var id = row["ID"]?.ToString();
            var code = row["部门编码"]?.ToString() ?? "";
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(code))
                oldIdToCode[id] = code;
        }

        var operatorId = GetUserId();

        for (int i = 0; i < table.Rows.Count; i++)
        {
            try
            {
                var row = table.Rows[i];
                var code = row["部门编码"]?.ToString() ?? "";

                // Check if department code already exists
                var existingDept = await _departmentService.GetByCodeAsync(code);
                if (existingDept != null)
                {
                    if (!overwrite)
                    {
                        duplicateCount++;
                        errors.Add($"第{i + 2}行: 部门编码 '{code}' 已存在");
                        continue;
                    }

                    // Update existing department
                    existingDept.Name = row["部门名称"]?.ToString() ?? "";
                    existingDept.SortOrder = int.TryParse(row["排序"]?.ToString(), out var sortVal) ? sortVal : 0;
                    existingDept.Status = row["状态"]?.ToString() == "0" ? false : true;

                    // Resolve parent by code instead of old ID
                    var pIdStr = row["上级部门ID"]?.ToString();
                    if (!string.IsNullOrEmpty(pIdStr) && oldIdToCode.TryGetValue(pIdStr, out var pCode))
                    {
                        var parent = await _departmentService.GetByCodeAsync(pCode);
                        existingDept.ParentId = parent?.Id;
                    }

                    var (updSuccess, updMsg) = await _departmentService.UpdateAsync(existingDept, operatorId);
                    if (updSuccess) successCount++;
                    else { failCount++; errors.Add($"第{i + 2}行: {updMsg}"); }
                    continue;
                }

                // Resolve parent by code instead of old ID
                long? resolvedParentId = null;
                var parentIdStr = row["上级部门ID"]?.ToString();
                if (!string.IsNullOrEmpty(parentIdStr) && oldIdToCode.TryGetValue(parentIdStr, out var parentCode))
                {
                    var parent = await _departmentService.GetByCodeAsync(parentCode);
                    if (parent != null)
                        resolvedParentId = parent.Id;
                }

                var dept = new Department
                {
                    Name = row["部门名称"]?.ToString() ?? "",
                    Code = code,
                    ParentId = resolvedParentId,
                    SortOrder = int.TryParse(row["排序"]?.ToString(), out var sort) ? sort : 0,
                    Status = row["状态"]?.ToString() == "0" ? false : true
                };

                var (success, msg) = await _departmentService.CreateAsync(dept, operatorId);
                if (success) successCount++;
                else { failCount++; errors.Add($"第{i + 2}行: {msg}"); }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"第{i + 2}行: 数据格式错误 - {ex.Message}");
            }
        }

        var msgSuffix = duplicateCount > 0 ? $", 重复{duplicateCount}条" : "";
        return Success(new { successCount, failCount, duplicateCount, errors }, $"导入完成: 成功{successCount}条, 失败{failCount}条{msgSuffix}");
    }

    private async Task<IActionResult> ImportEmployees(DataTable table, bool overwrite = false)
    {
        var successCount = 0;
        var failCount = 0;
        var duplicateCount = 0;
        var errors = new List<string>();

        // Load all departments to map code -> id and id -> exists
        var departments = await _departmentService.GetAllAsync();
        var deptCodeToId = departments
            .Where(d => !string.IsNullOrEmpty(d.Code))
            .ToDictionary(d => d.Code, d => d.Id);
        var deptIdSet = new HashSet<long>(departments.Select(d => d.Id));

        var hasDeptCodeCol = table.Columns.Contains("部门编码");
        var hasDeptIdCol = table.Columns.Contains("部门ID");

        var operatorId = GetUserId();

        for (int i = 0; i < table.Rows.Count; i++)
        {
            try
            {
                var row = table.Rows[i];
                var employeeNo = row["工号"]?.ToString() ?? "";

                // Check if employee number already exists
                var existingEmployee = await _employeeService.GetByEmployeeNoAsync(employeeNo);
                if (existingEmployee != null)
                {
                    if (!overwrite)
                    {
                        duplicateCount++;
                        errors.Add($"第{i + 2}行: 工号 '{employeeNo}' 已存在");
                        continue;
                    }

                    // Resolve department: try code first, then fall back to ID
                    long? empDeptId = null;
                    if (hasDeptCodeCol)
                    {
                        var deptCode = row["部门编码"]?.ToString();
                        if (!string.IsNullOrEmpty(deptCode) && deptCodeToId.TryGetValue(deptCode, out var deptIdByCode))
                            empDeptId = deptIdByCode;
                    }
                    if (!empDeptId.HasValue && hasDeptIdCol && long.TryParse(row["部门ID"]?.ToString(), out var oldDeptId) && deptIdSet.Contains(oldDeptId))
                        empDeptId = oldDeptId;

                    // Update existing employee
                    existingEmployee.Name = row["姓名"]?.ToString() ?? "";
                    existingEmployee.Gender = row["性别"]?.ToString();
                    existingEmployee.Phone = row["电话"]?.ToString();
                    existingEmployee.Email = row["邮箱"]?.ToString();
                    existingEmployee.DepartmentId = empDeptId;
                    existingEmployee.Position = row["职位"]?.ToString();
                    existingEmployee.HireDate = DateOnly.TryParse(row["入职日期"]?.ToString(), out var hireDateVal) ? hireDateVal : null;
                    existingEmployee.Status = row["状态"]?.ToString() == "0" ? false : true;

                    var (updSuccess, updMsg) = await _employeeService.UpdateAsync(existingEmployee, operatorId);
                    if (updSuccess) successCount++;
                    else { failCount++; errors.Add($"第{i + 2}行: {updMsg}"); }
                    continue;
                }

                // Resolve department: try code first, then fall back to ID
                long? resolvedDeptId = null;
                if (hasDeptCodeCol)
                {
                    var deptCode = row["部门编码"]?.ToString();
                    if (!string.IsNullOrEmpty(deptCode) && deptCodeToId.TryGetValue(deptCode, out var deptIdByCode))
                        resolvedDeptId = deptIdByCode;
                }
                if (!resolvedDeptId.HasValue && hasDeptIdCol && long.TryParse(row["部门ID"]?.ToString(), out var deptId) && deptIdSet.Contains(deptId))
                    resolvedDeptId = deptId;

                var employee = new Employee
                {
                    EmployeeNo = employeeNo,
                    Name = row["姓名"]?.ToString() ?? "",
                    Gender = row["性别"]?.ToString(),
                    Phone = row["电话"]?.ToString(),
                    Email = row["邮箱"]?.ToString(),
                    DepartmentId = resolvedDeptId,
                    Position = row["职位"]?.ToString(),
                    HireDate = DateOnly.TryParse(row["入职日期"]?.ToString(), out var hireDate) ? hireDate : null,
                    Status = row["状态"]?.ToString() == "0" ? false : true
                };

                var (success, msg) = await _employeeService.CreateAsync(employee, operatorId);
                if (success) successCount++;
                else { failCount++; errors.Add($"第{i + 2}行: {msg}"); }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"第{i + 2}行: 数据格式错误 - {ex.Message}");
            }
        }

        var msgSuffix = duplicateCount > 0 ? $", 重复{duplicateCount}条" : "";
        return Success(new { successCount, failCount, duplicateCount, errors }, $"导入完成: 成功{successCount}条, 失败{failCount}条{msgSuffix}");
    }

    private async Task<IActionResult> ImportUsers(DataTable table, bool overwrite = false)
    {
        var successCount = 0;
        var failCount = 0;
        var duplicateCount = 0;
        var errors = new List<string>();
        var operatorId = GetUserId();

        // Pre-load all employees for lookup
        var allEmployees = await _employeeService.GetAllAsync();
        var employeeByNo = allEmployees.ToDictionary(e => e.EmployeeNo, e => e);
        var employeeByName = allEmployees.GroupBy(e => e.Name)
            .ToDictionary(g => g.Key, g => g.First());

        for (int i = 0; i < table.Rows.Count; i++)
        {
            try
            {
                var row = table.Rows[i];
                var username = row["用户名"]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(username))
                {
                    failCount++;
                    errors.Add($"第{i + 2}行: 用户名为空");
                    continue;
                }

                // Resolve employee by employee_no (工号) first, then by name (姓名)
                long? resolvedEmployeeId = null;
                var employeeNo = row["员工工号"]?.ToString() ?? "";
                var employeeName = row["姓名"]?.ToString() ?? "";
                
                if (!string.IsNullOrWhiteSpace(employeeNo))
                {
                    if (employeeByNo.TryGetValue(employeeNo, out var emp))
                        resolvedEmployeeId = emp.Id;
                    else
                    {
                        failCount++;
                        errors.Add($"第{i + 2}行: 找不到工号为 '{employeeNo}' 的员工");
                        continue;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(employeeName))
                {
                    if (employeeByName.TryGetValue(employeeName, out var emp))
                        resolvedEmployeeId = emp.Id;
                }

                var existingUser = await _userService.GetByUsernameAsync(username);
                if (existingUser != null)
                {
                    if (!overwrite)
                    {
                        duplicateCount++;
                        errors.Add($"第{i + 2}行: 用户名 '{username}' 已存在");
                        continue;
                    }

                    existingUser.Status = row["状态"]?.ToString() == "0" ? false : true;
                    existingUser.EmployeeId = resolvedEmployeeId;

                    var password = row["密码"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(password))
                        existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

                    var (updSuccess, updMsg) = await _userService.UpdateAsync(existingUser, operatorId);
                    if (updSuccess) successCount++;
                    else { failCount++; errors.Add($"第{i + 2}行: {updMsg}"); }
                    continue;
                }

                var passwordHash = row["密码"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(passwordHash))
                {
                    failCount++;
                    errors.Add($"第{i + 2}行: 密码不能为空");
                    continue;
                }

                var user = new User
                {
                    Username = username,
                    PasswordHash = passwordHash,
                    EmployeeId = resolvedEmployeeId,
                    Status = row["状态"]?.ToString() == "0" ? false : true
                };

                var (success, msg, _) = await _userService.CreateAsync(user, operatorId);
                if (success) successCount++;
                else { failCount++; errors.Add($"第{i + 2}行: {msg}"); }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"第{i + 2}行: 数据格式错误 - {ex.Message}");
            }
        }

        var msgSuffix = duplicateCount > 0 ? $", 重复{duplicateCount}条" : "";
        return Success(new { successCount, failCount, duplicateCount, errors }, $"导入完成: 成功{successCount}条, 失败{failCount}条{msgSuffix}");
    }

    private async Task<IActionResult> ImportRoles(DataTable table, bool overwrite = false)
    {
        var successCount = 0;
        var failCount = 0;
        var duplicateCount = 0;
        var errors = new List<string>();
        var operatorId = GetUserId();

        for (int i = 0; i < table.Rows.Count; i++)
        {
            try
            {
                var row = table.Rows[i];
                var code = row["角色编码"]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(code))
                {
                    failCount++;
                    errors.Add($"第{i + 2}行: 角色编码为空");
                    continue;
                }

                var existingRole = await _roleService.GetByCodeAsync(code);
                if (existingRole != null)
                {
                    if (!overwrite)
                    {
                        duplicateCount++;
                        errors.Add($"第{i + 2}行: 角色编码 '{code}' 已存在");
                        continue;
                    }

                    existingRole.Name = row["角色名称"]?.ToString() ?? "";
                    existingRole.Description = row["描述"]?.ToString();
                    existingRole.Status = row["状态"]?.ToString() == "0" ? false : true;

                    var updRequest = new RoleUpdateRequest
                    {
                        Id = existingRole.Id,
                        Name = existingRole.Name,
                        Code = existingRole.Code,
                        Description = existingRole.Description,
                        Status = existingRole.Status
                    };

                    var (updSuccess, updMsg) = await _roleService.UpdateAsync(updRequest, operatorId);
                    if (updSuccess) successCount++;
                    else { failCount++; errors.Add($"第{i + 2}行: {updMsg}"); }
                    continue;
                }

                var roleCreateRequest = new RoleCreateRequest
                {
                    Name = row["角色名称"]?.ToString() ?? "",
                    Code = code,
                    Description = row["描述"]?.ToString(),
                    Status = row["状态"]?.ToString() == "0" ? false : true
                };

                var (success, msg) = await _roleService.CreateAsync(roleCreateRequest, operatorId);
                if (success) successCount++;
                else { failCount++; errors.Add($"第{i + 2}行: {msg}"); }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"第{i + 2}行: 数据格式错误 - {ex.Message}");
            }
        }

        var msgSuffix = duplicateCount > 0 ? $", 重复{duplicateCount}条" : "";
        return Success(new { successCount, failCount, duplicateCount, errors }, $"导入完成: 成功{successCount}条, 失败{failCount}条{msgSuffix}");
    }

    private async Task<IActionResult> ImportPermissions(DataTable table, bool overwrite = false)
    {
        var successCount = 0;
        var failCount = 0;
        var duplicateCount = 0;
        var errors = new List<string>();
        var operatorId = GetUserId();

        // First pass: collect all codes to build parent relationships
        var codeMapping = new Dictionary<string, string>();
        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var code = row["权限编码"]?.ToString() ?? "";
            var pCode = row["上级权限编码"]?.ToString() ?? "";
            if (!string.IsNullOrEmpty(code))
                codeMapping[code] = pCode;
        }

        for (int i = 0; i < table.Rows.Count; i++)
        {
            try
            {
                var row = table.Rows[i];
                var code = row["权限编码"]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(code))
                {
                    failCount++;
                    errors.Add($"第{i + 2}行: 权限编码为空");
                    continue;
                }

                var existingPerm = await _permissionService.GetByCodeAsync(code);
                if (existingPerm != null)
                {
                    if (!overwrite)
                    {
                        duplicateCount++;
                        errors.Add($"第{i + 2}行: 权限编码 '{code}' 已存在");
                        continue;
                    }

                    existingPerm.Name = row["权限名称"]?.ToString() ?? "";
                    existingPerm.Type = Enum.TryParse<PermissionType>(row["类型"]?.ToString(), true, out var updPermType) ? updPermType : PermissionType.Menu;
                    existingPerm.Path = row["路由路径"]?.ToString();
                    if (int.TryParse(row["排序"]?.ToString(), out var sortVal))
                        existingPerm.SortOrder = sortVal;

                    // Resolve parent by code
                    var parentCode = codeMapping.GetValueOrDefault(code);
                    long? updParentId = null;
                    if (!string.IsNullOrEmpty(parentCode))
                    {
                        var parent = await _permissionService.GetByCodeAsync(parentCode);
                        updParentId = parent?.Id;
                    }
                    else
                        updParentId = null;

                    var permUpdateRequest = new PermissionUpdateRequest
                    {
                        Id = existingPerm.Id,
                        Name = existingPerm.Name,
                        Code = existingPerm.Code,
                        Type = existingPerm.Type,
                        Path = existingPerm.Path,
                        SortOrder = existingPerm.SortOrder,
                        ParentId = updParentId
                    };

                    var (updSuccess, updMsg) = await _permissionService.UpdateAsync(permUpdateRequest, operatorId);
                    if (updSuccess) successCount++;
                    else { failCount++; errors.Add($"第{i + 2}行: {updMsg}"); }
                    continue;
                }

                // Resolve parent by code
                long? resolvedParentId = null;
                var parentCodeStr = codeMapping.GetValueOrDefault(code);
                if (!string.IsNullOrEmpty(parentCodeStr))
                {
                    var parent = await _permissionService.GetByCodeAsync(parentCodeStr);
                    if (parent != null)
                        resolvedParentId = parent.Id;
                }

                var permCreateRequest = new PermissionCreateRequest
                {
                    Name = row["权限名称"]?.ToString() ?? "",
                    Code = code,
                    Type = Enum.TryParse<PermissionType>(row["类型"]?.ToString(), true, out var newPermType) ? newPermType : PermissionType.Menu,
                    ParentId = resolvedParentId,
                    Path = row["路由路径"]?.ToString(),
                    SortOrder = int.TryParse(row["排序"]?.ToString(), out var sort) ? sort : 0
                };

                var (success, msg) = await _permissionService.CreateAsync(permCreateRequest, operatorId);
                if (success) successCount++;
                else { failCount++; errors.Add($"第{i + 2}行: {msg}"); }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"第{i + 2}行: 数据格式错误 - {ex.Message}");
            }
        }

        var msgSuffix = duplicateCount > 0 ? $", 重复{duplicateCount}条" : "";
        return Success(new { successCount, failCount, duplicateCount, errors }, $"导入完成: 成功{successCount}条, 失败{failCount}条{msgSuffix}");
    }

    private async Task<IActionResult> ImportAttendanceConfig(string module, DataTable table, bool overwrite = false)
    {
        var successCount = 0;
        var failCount = 0;
        var duplicateCount = 0;
        var errors = new List<string>();
        var operatorId = GetUserId();

        for (int i = 0; i < table.Rows.Count; i++)
        {
            try
            {
                var row = table.Rows[i];
                switch (module.ToLower())
                {
                    case "day-type":
                        {
                            var name = row["类型名称"]?.ToString() ?? "";
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                failCount++; errors.Add($"第{i + 2}行: 类型名称为空"); continue;
                            }
                            var existing = (await _attendanceRepository.GetDayTypesAsync())
                                .FirstOrDefault(d => d.DayTypeName == name);
                            if (existing != null)
                            {
                                if (!overwrite) { duplicateCount++; continue; }
                                existing.DayTypeCaption = row["说明"]?.ToString();
                                await _attendanceRepository.UpdateDayTypeAsync(existing);
                            }
                            else
                            {
                                await _attendanceRepository.AddDayTypeAsync(new AttendanceDayType
                                {
                                    DayTypeName = name,
                                    DayTypeCaption = row["说明"]?.ToString()
                                });
                            }
                        }
                        break;

                    case "scheme-class":
                        {
                            var name = row["类别名称"]?.ToString() ?? "";
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                failCount++; errors.Add($"第{i + 2}行: 类别名称为空"); continue;
                            }
                            var existing = (await _attendanceRepository.GetSchemeClassesAsync())
                                .FirstOrDefault(s => s.ClassName == name);
                            if (existing != null)
                            {
                                if (!overwrite) { duplicateCount++; continue; }
                                existing.Periods = int.TryParse(row["周期数"]?.ToString(), out var p) ? p : null;
                                existing.ClassPeriods = int.TryParse(row["班次数"]?.ToString(), out var cp) ? cp : null;
                                existing.ClassDescription = row["描述"]?.ToString();
                                await _attendanceRepository.UpdateSchemeClassAsync(existing);
                            }
                            else
                            {
                                await _attendanceRepository.AddSchemeClassAsync(new AttendanceSchemeClass
                                {
                                    ClassName = name,
                                    Periods = int.TryParse(row["周期数"]?.ToString(), out var p) ? p : null,
                                    ClassPeriods = int.TryParse(row["班次数"]?.ToString(), out var cp) ? cp : null,
                                    ClassDescription = row["描述"]?.ToString()
                                });
                            }
                        }
                        break;

                    case "plan-time":
                        {
                            var name = row["计划名称"]?.ToString() ?? "";
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                failCount++; errors.Add($"第{i + 2}行: 计划名称为空"); continue;
                            }
                            var existing = (await _attendanceRepository.GetPlanTimesAsync())
                                .FirstOrDefault(p => p.PlanName == name);
                            if (existing != null)
                            {
                                if (!overwrite) { duplicateCount++; continue; }
                                existing.DayTypeId = long.TryParse(row["日期类型ID"]?.ToString(), out var dtId) ? dtId : 0;
                                existing.Description = row["描述"]?.ToString();
                                if (TimeSpan.TryParse(row["上班时间"]?.ToString(), out var bTime))
                                    existing.BTime = bTime;
                                if (TimeSpan.TryParse(row["下班时间"]?.ToString(), out var eTime))
                                    existing.ETime = eTime;
                                await _attendanceRepository.UpdatePlanTimeAsync(existing);
                            }
                            else
                            {
                                await _attendanceRepository.AddPlanTimeAsync(new AttendancePlanTime
                                {
                                    PlanName = name,
                                    DayTypeId = long.TryParse(row["日期类型ID"]?.ToString(), out var dtId) ? dtId : 0,
                                    Description = row["描述"]?.ToString(),
                                    BTime = TimeSpan.TryParse(row["上班时间"]?.ToString(), out var bTime) ? bTime : TimeSpan.Zero,
                                    ETime = TimeSpan.TryParse(row["下班时间"]?.ToString(), out var eTime) ? eTime : TimeSpan.Zero
                                });
                            }
                        }
                        break;

                    case "holiday":
                        {
                            var dateStr = row["日期"]?.ToString() ?? "";
                            if (!DateTime.TryParse(dateStr, out var sDate))
                            {
                                failCount++; errors.Add($"第{i + 2}行: 日期格式错误"); continue;
                            }
                            var existing = (await _attendanceRepository.GetHolidaysAsync(null))
                                .FirstOrDefault(h => h.SDate.Date == sDate.Date);
                            if (existing != null)
                            {
                                if (!overwrite) { duplicateCount++; continue; }
                                existing.IYear = int.TryParse(row["年份"]?.ToString(), out var yr) ? yr : sDate.Year;
                                existing.SName = row["假日名称"]?.ToString();
                                existing.SDescription = row["描述"]?.ToString();
                                if (TimeSpan.TryParse(row["开始时间"]?.ToString(), out var bTime))
                                    existing.BTime = bTime;
                                if (TimeSpan.TryParse(row["结束时间"]?.ToString(), out var eTime))
                                    existing.ETime = eTime;
                                await _attendanceRepository.UpdateHolidayAsync(existing);
                            }
                            else
                            {
                                await _attendanceRepository.AddHolidayAsync(new AttendanceHoliday
                                {
                                    IYear = int.TryParse(row["年份"]?.ToString(), out var yr) ? yr : sDate.Year,
                                    SDate = sDate,
                                    SName = row["假日名称"]?.ToString(),
                                    SDescription = row["描述"]?.ToString(),
                                    BTime = TimeSpan.TryParse(row["开始时间"]?.ToString(), out var bTime) ? bTime : null,
                                    ETime = TimeSpan.TryParse(row["结束时间"]?.ToString(), out var eTime) ? eTime : null
                                });
                            }
                        }
                        break;

                    case "fee-calculator":
                        {
                            if (!long.TryParse(row["日期类型ID"]?.ToString(), out var dayTypeId))
                            {
                                failCount++; errors.Add($"第{i + 2}行: 日期类型ID格式错误"); continue;
                            }
                            var rangeA = int.TryParse(row["范围A(分钟)"]?.ToString(), out var ra) ? ra : 0;
                            var rangeB = int.TryParse(row["范围B(分钟)"]?.ToString(), out var rb) ? rb : 0;
                            var price = decimal.TryParse(row["金额(元)"]?.ToString(), out var pr) ? pr : 0m;
                            var existing = (await _attendanceRepository.GetFeeCalculatorsAsync(null))
                                .FirstOrDefault(f => f.DayTypeId == dayTypeId && f.RangeA == rangeA && f.RangeB == rangeB);
                            if (existing != null)
                            {
                                if (!overwrite) { duplicateCount++; continue; }
                                existing.RangePrice = price;
                                await _attendanceRepository.UpdateFeeCalculatorAsync(existing);
                            }
                            else
                            {
                                await _attendanceRepository.AddFeeCalculatorAsync(new AttendanceFeeCalculator
                                {
                                    DayTypeId = dayTypeId,
                                    RangeA = rangeA,
                                    RangeB = rangeB,
                                    RangePrice = price
                                });
                            }
                        }
                        break;

                    case "employee-class-ref":
                    {
                        if (!long.TryParse(row["员工ID"]?.ToString(), out var empId))
                        {
                            failCount++; errors.Add($"第{i + 2}行: 员工ID格式错误"); continue;
                        }
                        if (!long.TryParse(row["班次ID"]?.ToString(), out var classId))
                            {
                                failCount++; errors.Add($"第{i + 2}行: 班次ID格式错误"); continue;
                            }
                            if (!DateTime.TryParse(row["生效日期"]?.ToString(), out var effDate))
                            {
                                failCount++; errors.Add($"第{i + 2}行: 生效日期格式错误"); continue;
                            }
                            DateTime? expDate = DateTime.TryParse(row["失效日期"]?.ToString(), out var ed) ? ed : null;
                            var existing = (await _attendanceRepository.GetEmployeeRefSchemeClassesAsync(empId))
                                .FirstOrDefault(e => e.ClassId == classId && e.EffDate.Date == effDate.Date);
                            if (existing != null)
                            {
                                if (!overwrite) { duplicateCount++; continue; }
                                existing.PeriodNo = int.TryParse(row["周期序号"]?.ToString(), out var pn) ? pn : null;
                                existing.ExpDate = expDate;
                                await _attendanceRepository.UpdateEmployeeRefSchemeClassAsync(existing);
                            }
                            else
                            {
                                await _attendanceRepository.AddEmployeeRefSchemeClassAsync(new AttendanceEmployeeRefSchemeClass
                                {
                                    EmployeeId = empId,
                                    ClassId = classId,
                                    PeriodNo = int.TryParse(row["周期序号"]?.ToString(), out var pn) ? pn : null,
                                    EffDate = effDate,
                                    ExpDate = expDate
                                });
                            }
                        }
                        break;
                }
                successCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"第{i + 2}行: 数据格式错误 - {ex.Message}");
            }
        }

        return Success(new { successCount, failCount, duplicateCount, errors },
            $"导入完成: 成功{successCount}条, 失败{failCount}条{(duplicateCount > 0 ? $", 重复{duplicateCount}条" : "")}");
    }

    private static List<T> FlattenTree<T>(IEnumerable<T> items) where T : class
    {
        var result = new List<T>();
        void Walk(IEnumerable<T> list)
        {
            foreach (var item in list)
            {
                result.Add(item);
                var prop = typeof(T).GetProperty("Children");
                if (prop != null)
                {
                    var children = prop.GetValue(item) as IEnumerable<T>;
                    if (children != null)
                        Walk(children);
                }
            }
        }
        Walk(items);
        return result;
    }

    private static DataTable BuildDataTable<T>(IEnumerable<T> items, params (string columnName, Func<T, object?> valueGetter)[] columns)
    {
        var table = new DataTable();
        foreach (var (colName, _) in columns)
            table.Columns.Add(colName);
        foreach (var item in items)
        {
            var row = table.NewRow();
            for (int i = 0; i < columns.Length; i++)
                row[i] = columns[i].valueGetter(item)?.ToString() ?? "";
            table.Rows.Add(row);
        }
        return table;
    }
}