using EAMS2026.Application.Services.System;
using EAMS2026.Domain.DTOs.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;

namespace EAMS2026.Api.Controllers.System;

[Authorize]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(500)]
[Route("api/print")]
public class PrintController : BaseController
{
    private readonly PrintService _printService;
    private readonly DepartmentService _departmentService;
    private readonly EmployeeService _employeeService;
    private readonly UserService _userService;
    private readonly RoleService _roleService;

    public PrintController(
        PrintService printService,
        DepartmentService departmentService,
        EmployeeService employeeService,
        UserService userService,
        RoleService roleService)
    {
        _printService = printService;
        _departmentService = departmentService;
        _employeeService = employeeService;
        _userService = userService;
        _roleService = roleService;
    }

    [HttpGet("{module}")]
    public async Task<IActionResult> Print(string module)
    {
        string htmlContent;
        string fileName;

        switch (module.ToLower())
        {
            case "department":
                {
                    var data = await _departmentService.GetAllAsync();
                    htmlContent = BuildDepartmentHtml(data);
                    fileName = $"部门列表_{DateTime.Now:yyyyMMdd}.pdf";
                }
                break;

            case "employee":
                {
                    var data = await _employeeService.GetAllAsync();
                    htmlContent = BuildEmployeeHtml(data);
                    fileName = $"员工列表_{DateTime.Now:yyyyMMdd}.pdf";
                }
                break;

            case "user":
                {
                    var data = await _userService.GetAllAsync();
                    htmlContent = BuildUserHtml(data);
                    fileName = $"用户列表_{DateTime.Now:yyyyMMdd}.pdf";
                }
                break;

            case "role":
                {
                    var data = await _roleService.GetAllAsync();
                    htmlContent = BuildRoleHtml(data);
                    fileName = $"角色列表_{DateTime.Now:yyyyMMdd}.pdf";
                }
                break;

            default:
                return Fail($"不支持的打印模块: {module}");
        }

        var pdfBytes = await _printService.GeneratePdfAsync(htmlContent, false);
        return File(pdfBytes, "application/pdf", fileName);
    }

    private static string BuildDepartmentHtml(IEnumerable<Department> departments)
    {
        var sb = new StringBuilder();
        sb.Append(BuildHtmlHeader("部门列表"));
        sb.Append("<table><thead><tr><th>ID</th><th>部门名称</th><th>部门编码</th><th>排序</th><th>状态</th></tr></thead><tbody>");
        foreach (var dept in departments)
        {
            sb.Append($"<tr><td>{dept.Id}</td><td>{HtmlEncode(dept.Name)}</td><td>{HtmlEncode(dept.Code)}</td><td>{dept.SortOrder}</td><td>{(dept.Status ? "启用" : "禁用")}</td></tr>");
        }
        sb.Append("</tbody></table></body></html>");
        return sb.ToString();
    }

    private static string BuildEmployeeHtml(IEnumerable<Employee> employees)
    {
        var sb = new StringBuilder();
        sb.Append(BuildHtmlHeader("员工列表"));
        sb.Append("<table><thead><tr><th>ID</th><th>工号</th><th>姓名</th><th>性别</th><th>电话</th><th>职位</th><th>状态</th></tr></thead><tbody>");
        foreach (var emp in employees)
        {
            sb.Append($"<tr><td>{emp.Id}</td><td>{HtmlEncode(emp.EmployeeNo)}</td><td>{HtmlEncode(emp.Name)}</td><td>{emp.Gender}</td><td>{emp.Phone}</td><td>{HtmlEncode(emp.Position)}</td><td>{(emp.Status ? "在职" : "离职")}</td></tr>");
        }
        sb.Append("</tbody></table></body></html>");
        return sb.ToString();
    }

    private static string BuildUserHtml(IEnumerable<User> users)
    {
        var sb = new StringBuilder();
        sb.Append(BuildHtmlHeader("用户列表"));
        sb.Append("<table><thead><tr><th>ID</th><th>用户名</th><th>状态</th><th>最后登录</th></tr></thead><tbody>");
        foreach (var user in users)
        {
            sb.Append($"<tr><td>{user.Id}</td><td>{HtmlEncode(user.Username)}</td><td>{(user.Status ? "启用" : "禁用")}</td><td>{user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm")}</td></tr>");
        }
        sb.Append("</tbody></table></body></html>");
        return sb.ToString();
    }

    private static string BuildRoleHtml(IEnumerable<RoleDto> roles)
    {
        var sb = new StringBuilder();
        sb.Append(BuildHtmlHeader("角色列表"));
        sb.Append("<table><thead><tr><th>ID</th><th>角色名称</th><th>角色编码</th><th>描述</th><th>状态</th></tr></thead><tbody>");
        foreach (var role in roles)
        {
            sb.Append($"<tr><td>{role.Id}</td><td>{HtmlEncode(role.Name)}</td><td>{HtmlEncode(role.Code)}</td><td>{HtmlEncode(role.Description)}</td><td>{(role.Status ? "启用" : "禁用")}</td></tr>");
        }
        sb.Append("</tbody></table></body></html>");
        return sb.ToString();
    }

    private static string BuildHtmlHeader(string title)
    {
        return $@"<!DOCTYPE html><html><head><meta charset=""utf-8""><title>{title}</title>
<style>
body {{ font-family: 'Microsoft YaHei', Arial, sans-serif; margin: 20px; }}
h1 {{ text-align: center; color: #333; font-size: 18px; margin-bottom: 20px; }}
table {{ width: 100%; border-collapse: collapse; font-size: 12px; }}
th, td {{ border: 1px solid #ddd; padding: 6px 8px; text-align: left; }}
th {{ background-color: #f5f7fa; font-weight: bold; color: #333; }}
tr:nth-child(even) {{ background-color: #fafafa; }}
.footer {{ text-align: center; margin-top: 15px; font-size: 10px; color: #999; }}
</style></head><body>
<h1>{title}</h1>
<div class=""footer"">打印时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>";
    }

    private static string HtmlEncode(string? value)
    {
        return global::System.Net.WebUtility.HtmlEncode(value ?? "");
    }
}