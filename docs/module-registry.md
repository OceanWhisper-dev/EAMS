## 模块登记表

> 最后更新：2026-06-24
> 版本：v0.4.2026.0624

### 基础设施（无模块分组）

| 模块 | 后端目录 | 数据库表前缀 | API 路由 | 前端视图目录 | API 文件 | 说明 |
|------|---------|------------|---------|-------------|---------|------|
| BaseController | `Controllers/BaseController.cs` | - | - | - | - | 控制器基类，提供统一响应格式 |
| Jwt | `Common/Interfaces/IJwtService.cs` | - | - | - | - | JWT Token 服务（无 Controller） |
| Excel | `Infrastructure/Services/ExcelService.cs` | - | - | - | - | Excel 导出服务 |
| DynamicData | `Infrastructure/Services/DynamicDataEngine.cs` | - | - | - | - | 动态 SQL 查询引擎（报表模块使用） |

### 系统管理

| 模块 | 后端目录 | 数据库表前缀 | API 路由 | 前端视图目录 | API 文件 | 说明 |
|------|---------|------------|---------|-------------|---------|------|
| **System 模块组** | **Controllers/System/** | **-** | **/api/** | **views/system/** | **system.ts** | 系统管理各模块 |
| ├─ Auth | Controllers/System/AuthController.cs | - | /api/auth | Login.vue, Profile.vue | system.ts | 认证/登录 |
| ├─ Department | Controllers/System/DepartmentController.cs | - | /api/department | system/Department.vue | system.ts | 部门管理 |
| ├─ Employee | Controllers/System/EmployeeController.cs | - | /api/employee | system/Employee.vue | system.ts | 员工管理 |
| ├─ User | Controllers/System/UserController.cs | - | /api/user | system/User.vue | system.ts | 用户管理 |
| ├─ Role | Controllers/System/RoleController.cs | - | /api/role | system/Role.vue | system.ts | 角色管理 |
| ├─ Permission | Controllers/System/PermissionController.cs | - | /api/permission | system/Permission.vue | system.ts | 权限管理 |
| ├─ Dict | Controllers/System/DictController.cs | - | /api/dict | system/Dict.vue | system.ts | 字典管理 |
| ├─ Dashboard | Controllers/System/DashboardController.cs | - | /api/dashboard | Dashboard.vue | dashboard.ts | 仪表盘 |
| ├─ DashboardConfig | - | - | - | system/DashboardConfig.vue | - | 仪表盘配置（复用 Dashboard 接口） |
| ├─ Message | Controllers/System/MessageController.cs | - | /api/message | message/Inbox.vue 等 | system.ts | 消息中心 |
| ├─ DataPermission | Controllers/System/DataPermissionController.cs | - | /api/data-permission | attendance/DataPermission.vue | attendance.ts | 数据权限（API 放在 attendance.ts） |
| ├─ OperationLog | Controllers/System/OperationLogController.cs | - | /api/operation-log | system/OperationLog.vue | system.ts | 操作日志 |
| ├─ ImportExport | Controllers/System/ImportExportController.cs | - | /api/import-export | - | system.ts | 导入导出（无前端页面） |
| └─ Print | - | - | - | - | system.ts | 打印服务（无 Controller） |

### 考勤管理

| 模块 | 后端目录 | 数据库表前缀 | API 路由 | 前端视图目录 | API 文件 | 说明 |
|------|---------|------------|---------|-------------|---------|------|
| **Attendance 模块组** | **Controllers/Attendance/** | **attendance_** | **/api/attendance** | **views/attendance/** | **attendance.ts** | 考勤管理各模块 |
| ├─ Attendance (核心) | Controllers/Attendance/AttendanceController.cs | attendance_ | /api/attendance | attendance/Report.vue 等 | attendance.ts | 考勤管理（含 9 个子页面） |
| └─ AttendanceCalculation | - | attendance_ | - | - | attendance.ts | 考勤计算服务（无 Controller） |

### ERP

| 模块 | 后端目录 | 数据库表前缀 | API 路由 | 前端视图目录 | API 文件 | 说明 |
|------|---------|------------|---------|-------------|---------|------|
| **ERP 模块组** | **Controllers/Erp/** | **erp_** | **/api/erp** | **views/erp/ views/report/** | **erp.ts report.ts erpSettings.ts** | ERP 辅助功能 |
| ├─ Report (报表管理) | Controllers/Erp/ReportController.cs | **erp_rpt_** | /api/erp/reports | report/ | report.ts | 报表管理（列表、设计器、查看器、数据源、业务员映射） |
| ├─ VouchModify (单据修改) | Controllers/Erp/VouchModifyController.cs | **erp_vouch_modify_** | /api/erp/vouch-modify | erp/ | erp.ts | ERP 单据修改（日期/客户修改及日志） |
| └─ ErpSettings (ERP 设置) | Controllers/Erp/ErpSettingsController.cs | erp_settings_ | /api/erp/settings | report/DataSourceManage.vue report/SalespersonMap.vue | erpSettings.ts | ERP 数据源配置与业务员对照 |

### 遗留/未实现模块

| 模块 | 状态 | 说明 |
|------|------|------|
| DingTalk | 仅接口定义 | `IDingTalkService` 存在，无 Controller 及 Service 实现 |
| HwattImport | 仅接口定义 | `IHwattImportService` 存在，无 Controller 及 Service 实现 |
| Test | 测试用 | `views/test/` 下 3 个测试页面，仅开发调试使用 |

### API 文件分布

| API 文件 | 所属模块 | 当前包含的 API |
|---------|---------|---------------|
| `system.ts` | 系统管理 | Auth, Department, Employee, User, Role, Permission, Dict, Dashboard, Message, DataPermission, OperationLog, ImportExport, Print |
| `dashboard.ts` | 系统管理/仪表盘 | Dashboard |
| `attendance.ts` | 考勤管理 | Attendance 全部 + DataPermission |
| `erp.ts` | ERP/单据修改 | VouchModify 全部 |
| `report.ts` | ERP/报表管理 | Report 全部（报表、透视表、书签、执行日志） |
| `erpSettings.ts` | ERP/ERP设置 | 数据源、业务员映射 |

### 数据库表前缀汇总

| 前缀 | 所属模块 | 涉及表 |
|------|---------|-------|
| `erp_rpt_` | ERP > 报表管理 | `erp_rpt_category`, `erp_rpt_report`, `erp_rpt_field`, `erp_rpt_filter`, `erp_rpt_sort`, `erp_rpt_chart`, `erp_rpt_permission`, `erp_rpt_field_permission`, `erp_rpt_bookmark`, `erp_rpt_execution_log`, `erp_rpt_datasource`, `erp_rpt_salesperson_map`, `erp_rpt_pivot_view`, `erp_rpt_pivot_view_share` |
| `erp_vouch_modify_` | ERP > 单据修改 | `erp_vouch_modify_logs` |
| `attendance_` | 考勤管理 | （各考勤相关表） |