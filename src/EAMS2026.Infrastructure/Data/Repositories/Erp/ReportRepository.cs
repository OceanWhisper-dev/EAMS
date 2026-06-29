using Dapper;
using EAMS2026.Domain.DTOs.Erp.Report;
using EAMS2026.Domain.Entities.Erp.Report;
using EAMS2026.Domain.Interfaces.Repositories.Erp;
using EAMS2026.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;
using System.Text.Json;

namespace EAMS2026.Infrastructure.Data.Repositories.Erp;

public class ReportRepository : IReportRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<ReportRepository> _logger;

    public ReportRepository(DbConnectionFactory connectionFactory, ILogger<ReportRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    // ========== 报表分类 ==========

    public async Task<IEnumerable<ReportCategoryDto>> GetCategoriesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT c.id, c.name, c.parent_id AS ParentId, c.sort_order AS SortOrder,
                           (SELECT COUNT(*) FROM erp_rpt_report r WHERE r.category_id = c.id AND r.is_deleted = FALSE AND r.status = 'published') AS ReportCount
                    FROM erp_rpt_category c
                    WHERE c.is_deleted = FALSE
                    ORDER BY c.sort_order, c.id";
        var categories = (await conn.QueryAsync<ReportCategoryDto>(sql)).ToList();
        return BuildCategoryTree(categories);
    }

    private static List<ReportCategoryDto> BuildCategoryTree(List<ReportCategoryDto> flat)
    {
        var lookup = flat.ToDictionary(x => x.Id);
        var roots = new List<ReportCategoryDto>();
        foreach (var item in flat)
        {
            if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
                parent.Children.Add(item);
            else
                roots.Add(item);
        }
        return roots;
    }

    public async Task<long> AddCategoryAsync(string name, long? parentId, int sortOrder, long operatorId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO erp_rpt_category (name, parent_id, sort_order, created_by, updated_by)
                    VALUES (@Name, @ParentId, @SortOrder, @OperatorId, @OperatorId)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, new { Name = name, ParentId = parentId, SortOrder = sortOrder, OperatorId = operatorId });
    }

    public async Task<bool> UpdateCategoryAsync(long id, string name, long? parentId, int sortOrder, long operatorId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE erp_rpt_category SET name = @Name, parent_id = @ParentId, sort_order = @SortOrder,
                        updated_by = @OperatorId, updated_at = NOW()
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, new { Id = id, Name = name, ParentId = parentId, SortOrder = sortOrder, OperatorId = operatorId }) > 0;
    }

    public async Task<bool> DeleteCategoryAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        // 检查是否有子分类或报表
        var hasChildren = await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM erp_rpt_category WHERE parent_id = @Id AND is_deleted = FALSE)", new { Id = id });
        var hasReports = await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM erp_rpt_report WHERE category_id = @Id AND is_deleted = FALSE)", new { Id = id });
        if (hasChildren || hasReports) return false;

        return await conn.ExecuteAsync(
            "UPDATE erp_rpt_category SET is_deleted = TRUE WHERE id = @Id", new { Id = id }) > 0;
    }

    // ========== 报表 ==========

    public async Task<IEnumerable<ReportDto>> GetReportsAsync(long? categoryId, long? userId = null)
    {
        using var conn = _connectionFactory.CreateConnection();

        // 检查当前用户是否为超级管理员
        var isAdmin = false;
        if (userId.HasValue)
        {
            isAdmin = await conn.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM sys_user_roles WHERE user_id = @UserId AND role_id = 1)",
                new { UserId = userId.Value });
        }

        var sql = @"SELECT r.id, r.name, r.title, r.description, r.category_id AS CategoryId,
                           c.name AS CategoryName, r.query_type AS QueryType,
                           r.query_datasource AS QueryDatasource,
                           ds.display_name AS QueryDatasourceName,
                           r.status,
                           r.is_system AS IsSystem, r.created_at AS CreatedAt, r.created_by AS CreatedBy,
                           r.updated_at AS UpdatedAt
                    FROM erp_rpt_report r
                    LEFT JOIN erp_rpt_category c ON r.category_id = c.id
                    LEFT JOIN erp_settings_datasource ds ON r.query_datasource = ds.name
                    WHERE r.is_deleted = FALSE";
        if (categoryId.HasValue)
            sql += " AND r.category_id = @CategoryId";
        // 非管理员只能看到已发布的报表
        if (!isAdmin)
            sql += " AND r.status = 'published'";
        // 非管理员按权限过滤：必须有匹配的 view 权限记录才能看到
        if (!isAdmin && userId.HasValue)
        {
            sql += @" AND EXISTS (SELECT 1 FROM erp_rpt_permission p WHERE p.report_id = r.id AND p.access_type = 'view'
                          AND ((p.principal_type = 'user' AND p.principal_id = @UserId)
                            OR (p.principal_type = 'role' AND p.principal_id IN (
                                SELECT ur.role_id FROM sys_user_roles ur WHERE ur.user_id = @UserId))))";
        }
        sql += " ORDER BY r.updated_at DESC";

        var sqlParams = new DynamicParameters();
        sqlParams.Add("CategoryId", categoryId);
        if (userId.HasValue)
            sqlParams.Add("UserId", userId.Value);

        var reports = (await conn.QueryAsync<ReportDto>(sql, sqlParams)).ToList();

        // 标记收藏 & 管理权限
        if (userId.HasValue)
        {
            var bookmarkedIds = await conn.QueryAsync<long>(
                "SELECT report_id FROM erp_rpt_bookmark WHERE user_id = @UserId", new { UserId = userId.Value });
            var bmSet = new HashSet<long>(bookmarkedIds);

            // 查询用户有 manage 权限的所有报表
            var managedIds = await conn.QueryAsync<long>(
                @"SELECT p.report_id FROM erp_rpt_permission p
                  WHERE p.access_type = 'manage'
                    AND ((p.principal_type = 'user' AND p.principal_id = @UserId)
                      OR (p.principal_type = 'role' AND p.principal_id IN (
                          SELECT ur.role_id FROM sys_user_roles ur WHERE ur.user_id = @UserId)))",
                new { UserId = userId.Value });
            var manageSet = new HashSet<long>(managedIds);

            foreach (var r in reports)
            {
                r.IsBookmarked = bmSet.Contains(r.Id);
                r.CanManage = isAdmin || manageSet.Contains(r.Id);
            }
        }

        return reports;
    }

    public async Task<ReportDetailDto?> GetReportDetailAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        var report = await conn.QueryFirstOrDefaultAsync<ReportDetailDto>(
            @"SELECT r.id, r.name, r.title, r.description, r.category_id AS CategoryId, c.name AS CategoryName,
                     r.query_type AS QueryType, r.query_text AS QueryText, r.query_datasource AS QueryDatasource,
                     r.is_system AS IsSystem, r.status, r.default_tab AS DefaultTab,
                     r.created_at AS CreatedAt, r.updated_at AS UpdatedAt
              FROM erp_rpt_report r
              LEFT JOIN erp_rpt_category c ON r.category_id = c.id
              WHERE r.id = @Id AND r.is_deleted = FALSE", new { Id = id }, tx);
        if (report == null) return null;

        report.Fields = (await conn.QueryAsync<ReportFieldDto>(
            @"SELECT id, report_id AS ReportId, field_name AS FieldName, field_title AS FieldTitle,
                     field_type AS FieldType, sort_order AS SortOrder, width, align,
                     is_display AS IsDisplay, is_sortable AS IsSortable, is_filterable AS IsFilterable,
                     is_groupable AS IsGroupable, is_summary AS IsSummary, summary_type AS SummaryType,
                     format_pattern AS FormatPattern
              FROM erp_rpt_field WHERE report_id = @Id AND is_deleted = FALSE ORDER BY sort_order", new { Id = id }, tx)).ToList();

        report.Filters = (await conn.QueryAsync<ReportFilterDto>(
            @"SELECT id, report_id AS ReportId, field_name AS FieldName, label,
                     operator, default_value AS DefaultValue, control_type AS ControlType,
                     options_query AS OptionsQuery, sort_order AS SortOrder, is_required AS IsRequired
              FROM erp_rpt_filter WHERE report_id = @Id AND is_deleted = FALSE ORDER BY sort_order", new { Id = id }, tx)).ToList();

        report.Sorts = (await conn.QueryAsync<ReportSortDto>(
            @"SELECT id, field_name AS FieldName, direction, sort_order AS SortOrder
              FROM erp_rpt_sort WHERE report_id = @Id ORDER BY sort_order", new { Id = id }, tx)).ToList();

        report.Charts = (await conn.QueryAsync<ReportChartDto>(
            @"SELECT id, chart_type AS ChartType, title, x_field AS XField, y_fields AS YFields,
                     group_field AS GroupField, options, sort_order AS SortOrder
              FROM erp_rpt_chart WHERE report_id = @Id AND is_deleted = FALSE ORDER BY sort_order", new { Id = id }, tx)).ToList();

        tx.Commit();
        return report;
    }

    public async Task<long> AddReportAsync(ReportDetailDto report, long operatorId)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var sql = @"INSERT INTO erp_rpt_report (name, title, description, category_id, query_type, query_text, query_datasource, status, default_tab, created_by, updated_by)
                        VALUES (@Name, @Title, @Description, @CategoryId, @QueryType, @QueryText, @QueryDatasource, @Status, @DefaultTab, @OperatorId, @OperatorId)
                        RETURNING id";
            var reportId = await conn.ExecuteScalarAsync<long>(sql, new
            {
                report.Name, report.Title, report.Description, report.CategoryId,
                report.QueryType, report.QueryText, report.QueryDatasource,
                report.DefaultTab, Status = "draft", OperatorId = operatorId
            }, tx);

            // 保存字段
            if (report.Fields?.Any() == true)
            {
                foreach (var f in report.Fields.OrderBy(x => x.SortOrder))
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO erp_rpt_field (report_id, field_name, field_title, field_type, sort_order, width, align,
                          is_display, is_sortable, is_filterable, is_groupable, is_summary, summary_type, format_pattern)
                          VALUES (@ReportId, @FieldName, @FieldTitle, @FieldType, @SortOrder, @Width, @Align,
                          @IsDisplay, @IsSortable, @IsFilterable, @IsGroupable, @IsSummary, @SummaryType, @FormatPattern)",
                        new { ReportId = reportId, f.FieldName, f.FieldTitle, f.FieldType, f.SortOrder, f.Width, f.Align,
                            f.IsDisplay, f.IsSortable, f.IsFilterable, f.IsGroupable, f.IsSummary, f.SummaryType, f.FormatPattern }, tx);
                }
            }

            // 保存过滤条件
            if (report.Filters?.Any() == true)
            {
                foreach (var f in report.Filters.OrderBy(x => x.SortOrder))
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO erp_rpt_filter (report_id, field_name, label, operator, default_value, control_type, options_query, sort_order, is_required)
                          VALUES (@ReportId, @FieldName, @Label, @Operator, @DefaultValue, @ControlType, @OptionsQuery, @SortOrder, @IsRequired)",
                        new { ReportId = reportId, f.FieldName, f.Label, f.Operator, f.DefaultValue, f.ControlType, f.OptionsQuery, f.SortOrder, f.IsRequired }, tx);
                }
            }

            // 保存排序
            if (report.Sorts?.Any() == true)
            {
                foreach (var s in report.Sorts.OrderBy(x => x.SortOrder))
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO erp_rpt_sort (report_id, field_name, direction, sort_order) VALUES (@ReportId, @FieldName, @Direction, @SortOrder)",
                        new { ReportId = reportId, s.FieldName, s.Direction, s.SortOrder }, tx);
                }
            }

            // 保存图表配置
            if (report.Charts?.Any() == true)
            {
                foreach (var c in report.Charts.OrderBy(x => x.SortOrder))
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO erp_rpt_chart (report_id, chart_type, title, x_field, y_fields, group_field, options, sort_order)
                          VALUES (@ReportId, @ChartType, @Title, @XField, @YFields::jsonb, @GroupField, @Options::jsonb, @SortOrder)",
                        new { ReportId = reportId, c.ChartType, c.Title, c.XField, c.YFields, c.GroupField, c.Options, c.SortOrder }, tx);
                }
            }

            tx.Commit();
            return reportId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateReportAsync(ReportDetailDto report, long operatorId)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            // 更新主表
            await conn.ExecuteAsync(
                @"UPDATE erp_rpt_report SET name = @Name, title = @Title, description = @Description,
                  category_id = @CategoryId, query_type = @QueryType, query_text = @QueryText,
                  query_datasource = @QueryDatasource, status = @Status, default_tab = @DefaultTab,
                  updated_by = @OperatorId, updated_at = NOW()
                  WHERE id = @Id AND is_deleted = FALSE",
                new { report.Id, report.Name, report.Title, report.Description, report.CategoryId,
                    report.QueryType, report.QueryText, report.QueryDatasource, report.Status, report.DefaultTab, OperatorId = operatorId }, tx);

            // 清空子表重新插入
            await conn.ExecuteAsync("UPDATE erp_rpt_field SET is_deleted = TRUE WHERE report_id = @Id", new { report.Id }, tx);
            await conn.ExecuteAsync("UPDATE erp_rpt_filter SET is_deleted = TRUE WHERE report_id = @Id", new { report.Id }, tx);
            await conn.ExecuteAsync("DELETE FROM erp_rpt_sort WHERE report_id = @Id", new { report.Id }, tx);
            await conn.ExecuteAsync("UPDATE erp_rpt_chart SET is_deleted = TRUE WHERE report_id = @Id", new { report.Id }, tx);

            // 重新插入字段
            if (report.Fields?.Any() == true)
            {
                foreach (var f in report.Fields.OrderBy(x => x.SortOrder))
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO erp_rpt_field (report_id, field_name, field_title, field_type, sort_order, width, align,
                          is_display, is_sortable, is_filterable, is_groupable, is_summary, summary_type, format_pattern)
                          VALUES (@ReportId, @FieldName, @FieldTitle, @FieldType, @SortOrder, @Width, @Align,
                          @IsDisplay, @IsSortable, @IsFilterable, @IsGroupable, @IsSummary, @SummaryType, @FormatPattern)",
                        new { ReportId = report.Id, f.FieldName, f.FieldTitle, f.FieldType, f.SortOrder, f.Width, f.Align,
                            f.IsDisplay, f.IsSortable, f.IsFilterable, f.IsGroupable, f.IsSummary, f.SummaryType, f.FormatPattern }, tx);
                }
            }

            // 重新插入过滤条件
            if (report.Filters?.Any() == true)
            {
                foreach (var f in report.Filters.OrderBy(x => x.SortOrder))
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO erp_rpt_filter (report_id, field_name, label, operator, default_value, control_type, options_query, sort_order, is_required)
                          VALUES (@ReportId, @FieldName, @Label, @Operator, @DefaultValue, @ControlType, @OptionsQuery, @SortOrder, @IsRequired)",
                        new { ReportId = report.Id, f.FieldName, f.Label, f.Operator, f.DefaultValue, f.ControlType, f.OptionsQuery, f.SortOrder, f.IsRequired }, tx);
                }
            }

            // 重新插入排序
            if (report.Sorts?.Any() == true)
            {
                foreach (var s in report.Sorts.OrderBy(x => x.SortOrder))
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO erp_rpt_sort (report_id, field_name, direction, sort_order) VALUES (@ReportId, @FieldName, @Direction, @SortOrder)",
                        new { ReportId = report.Id, s.FieldName, s.Direction, s.SortOrder }, tx);
                }
            }

            // 重新插入图表
            if (report.Charts?.Any() == true)
            {
                foreach (var c in report.Charts.OrderBy(x => x.SortOrder))
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO erp_rpt_chart (report_id, chart_type, title, x_field, y_fields, group_field, options, sort_order)
                          VALUES (@ReportId, @ChartType, @Title, @XField, @YFields::jsonb, @GroupField, @Options::jsonb, @SortOrder)",
                        new { ReportId = report.Id, c.ChartType, c.Title, c.XField, c.YFields, c.GroupField, c.Options, c.SortOrder }, tx);
                }
            }

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteReportAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE erp_rpt_report SET is_deleted = TRUE WHERE id = @Id AND is_system = FALSE",
            new { Id = id }) > 0;
    }

    public async Task<bool> UpdateReportStatusAsync(long id, string status)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE erp_rpt_report SET status = @Status, updated_at = NOW() WHERE id = @Id AND is_deleted = FALSE",
            new { Id = id, Status = status }) > 0;
    }

    // ========== 收藏 ==========

    public async Task<IEnumerable<ReportDto>> GetBookmarksAsync(long userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT r.id, r.name, r.title, r.description, r.category_id AS CategoryId,
                           c.name AS CategoryName, r.query_type AS QueryType, r.status,
                           r.is_system AS IsSystem, r.created_at AS CreatedAt, r.created_by AS CreatedBy,
                           r.updated_at AS UpdatedAt, TRUE AS IsBookmarked
                    FROM erp_rpt_bookmark b
                    INNER JOIN erp_rpt_report r ON b.report_id = r.id
                    LEFT JOIN erp_rpt_category c ON r.category_id = c.id
                    WHERE b.user_id = @UserId AND r.is_deleted = FALSE
                    ORDER BY b.sort_order, b.created_at";
        return await conn.QueryAsync<ReportDto>(sql, new { UserId = userId });
    }

    public async Task<bool> AddBookmarkAsync(long userId, long reportId)
    {
        using var conn = _connectionFactory.CreateConnection();
        try
        {
            await conn.ExecuteAsync(
                "INSERT INTO erp_rpt_bookmark (user_id, report_id) VALUES (@UserId, @ReportId)",
                new { UserId = userId, ReportId = reportId });
            return true;
        }
        catch (PostgresException) { return false; } // 唯一冲突
    }

    public async Task<bool> RemoveBookmarkAsync(long userId, long reportId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync(
            "DELETE FROM erp_rpt_bookmark WHERE user_id = @UserId AND report_id = @ReportId",
            new { UserId = userId, ReportId = reportId }) > 0;
    }

    // ========== 执行日志 ==========

    public async Task<long> AddExecutionLogAsync(long reportId, long userId, string? paramsJson, int rowCount, int durationMs, bool isSuccess, string? errorMessage)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO erp_rpt_execution_log (report_id, user_id, params, row_count, duration_ms, is_success, error_message)
                    VALUES (@ReportId, @UserId, @Params::jsonb, @RowCount, @DurationMs, @IsSuccess, @ErrorMessage)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, new
        {
            ReportId = reportId, UserId = userId, Params = paramsJson ?? "{}",
            RowCount = rowCount, DurationMs = durationMs, IsSuccess = isSuccess, ErrorMessage = errorMessage
        });
    }

    // ========== 权限 ==========

    public async Task<bool> HasPermissionAsync(long reportId, long userId, string accessType)
    {
        using var conn = _connectionFactory.CreateConnection();

        // 检查用户是否超级管理员（role_id=1），超级管理员拥有全部权限
        var isSuperAdmin = await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM sys_user_roles WHERE user_id = @UserId AND role_id = 1)",
            new { UserId = userId });
        if (isSuperAdmin)
        {
            _logger.LogInformation("[Permission] reportId={ReportId}, userId={UserId}, accessType={AccessType} → 超级管理员，自动授权", reportId, userId, accessType);
            return true;
        }

        // 检查是否有匹配的权限记录
        var sql = @"SELECT EXISTS(
            SELECT 1 FROM erp_rpt_permission p
            WHERE p.report_id = @ReportId AND p.access_type = @AccessType
              AND ((p.principal_type = 'user' AND p.principal_id = @UserId)
                OR (p.principal_type = 'role' AND p.principal_id IN (
                    SELECT ur.role_id FROM sys_user_roles ur WHERE ur.user_id = @UserId
                ))))";
        var result = await conn.ExecuteScalarAsync<bool>(sql, new { ReportId = reportId, UserId = userId, AccessType = accessType });
        _logger.LogInformation("[Permission] reportId={ReportId}, userId={UserId}, accessType={AccessType} → 检查结果={Result}", reportId, userId, accessType, result);
        return result;
    }

    public async Task<bool> SetPermissionAsync(long reportId, string principalType, long principalId, string accessType)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO erp_rpt_permission (report_id, principal_type, principal_id, access_type)
                    VALUES (@ReportId, @PrincipalType, @PrincipalId, @AccessType)
                    ON CONFLICT (report_id, principal_type, principal_id, access_type) DO NOTHING";
        return await conn.ExecuteAsync(sql, new { ReportId = reportId, PrincipalType = principalType, PrincipalId = principalId, AccessType = accessType }) > 0;
    }

    public async Task<bool> RemovePermissionAsync(long reportId, string principalType, long principalId, string accessType)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync(
            "DELETE FROM erp_rpt_permission WHERE report_id = @ReportId AND principal_type = @PrincipalType AND principal_id = @PrincipalId AND access_type = @AccessType",
            new { ReportId = reportId, PrincipalType = principalType, PrincipalId = principalId, AccessType = accessType }) > 0;
    }

    public async Task<IEnumerable<ReportPermission>> GetPermissionsAsync(long reportId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<ReportPermission>(
            "SELECT * FROM erp_rpt_permission WHERE report_id = @ReportId", new { ReportId = reportId });
    }

    public async Task<bool> DeletePermissionByIdAsync(long permissionId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync(
            "DELETE FROM erp_rpt_permission WHERE id = @Id", new { Id = permissionId }) > 0;
    }

    // ========== 数据源配置 ==========

    public async Task<IEnumerable<ReportDataSourceDto>> GetDataSourcesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<ReportDataSourceDto>(
            @"SELECT id, name, display_name AS DisplayName, db_type AS DbType, connection_string AS ConnectionString,
                     description, sort_order AS SortOrder, is_enabled AS IsEnabled, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM erp_settings_datasource
              WHERE is_deleted = FALSE
              ORDER BY sort_order, id");
    }

    public async Task<ReportDataSourceDto?> GetDataSourceAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ReportDataSourceDto>(
            @"SELECT id, name, display_name AS DisplayName, db_type AS DbType, connection_string AS ConnectionString,
                     description, sort_order AS SortOrder, is_enabled AS IsEnabled, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM erp_settings_datasource
              WHERE id = @Id AND is_deleted = FALSE", new { Id = id });
    }

    public async Task<ReportDataSourceDto?> GetDataSourceByNameAsync(string name)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ReportDataSourceDto>(
            @"SELECT id, name, display_name AS DisplayName, db_type AS DbType, connection_string AS ConnectionString,
                     description, sort_order AS SortOrder, is_enabled AS IsEnabled, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM erp_settings_datasource
              WHERE name = @Name AND is_deleted = FALSE", new { Name = name });
    }

    public async Task<long> AddDataSourceAsync(CreateDataSourceRequest request, long operatorId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO erp_settings_datasource (name, display_name, db_type, connection_string, description, sort_order, is_enabled, created_by, updated_by)
                    VALUES (@Name, @DisplayName, @DbType, @ConnectionString, @Description, @SortOrder, @IsEnabled, @OperatorId, @OperatorId)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, new
        {
            request.Name, request.DisplayName, request.DbType, request.ConnectionString,
            request.Description, request.SortOrder, request.IsEnabled, OperatorId = operatorId
        });
    }

    public async Task<bool> UpdateDataSourceAsync(long id, UpdateDataSourceRequest request, long operatorId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE erp_settings_datasource
                    SET display_name = @DisplayName, db_type = @DbType, connection_string = @ConnectionString,
                        description = @Description, sort_order = @SortOrder, is_enabled = @IsEnabled,
                        updated_at = NOW(), updated_by = @OperatorId
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, new
        {
            Id = id, request.DisplayName, request.DbType, request.ConnectionString,
            request.Description, request.SortOrder, request.IsEnabled, OperatorId = operatorId
        }) > 0;
    }

    public async Task<bool> DeleteDataSourceAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE erp_settings_datasource SET is_deleted = TRUE, updated_at = NOW() WHERE id = @Id", new { Id = id }) > 0;
    }

    public async Task<bool> TestConnectionAsync(string dbType, string connectionString)
    {
        if (dbType == "postgresql")
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            return true;
        }
        else if (dbType == "sqlserver")
        {
            if (global::System.OperatingSystem.IsLinux())
            {
                // Linux 上使用 ODBC + FreeTDS 连接 SQL Server 2005（兼容 TLS 1.0 问题）
                var odbcStr = ErpConnectionFactory.ConvertToOdbcConnectionString(connectionString);
                await using var conn = new global::System.Data.Odbc.OdbcConnection(odbcStr);
                await conn.OpenAsync();
                return true;
            }
            else
            {
                // Windows 上直接使用 System.Data.SqlClient
#pragma warning disable CS0618
                var connStr = ErpConnectionFactory.AppendSqlServerCompat(connectionString);
                await using var conn = new global::System.Data.SqlClient.SqlConnection(connStr);
#pragma warning restore CS0618
                await conn.OpenAsync();
                return true;
            }
        }
        return false;
    }

    // ========== 透视表配置 ==========

    public async Task<List<ReportPivotDto>> GetPivotViewsAsync(long reportId, long userId)
    {
        using var conn = _connectionFactory.CreateConnection();

        // 获取当前用户的角色ID列表
        var roleIds = (await conn.QueryAsync<long>(
            @"SELECT role_id FROM sys_user_roles WHERE user_id = @UserId",
            new { UserId = userId })).ToList();

        // 获取共享给当前用户或当前用户角色的配置，加上自己的配置
        var pivots = await conn.QueryAsync<ReportPivotDto>(
            @"SELECT DISTINCT p.autoid AS Id, p.report_id AS ReportId, p.user_id AS UserId,
                     p.pivot_name AS PivotName, p.pivot_params AS PivotParams,
                     p.is_last AS IsLast, FALSE AS IsDefault,
                     p.is_shared AS IsShared,
                     p.created_by_name AS CreatorName,
                     p.created_at AS CreatedAt, p.updated_at AS UpdatedAt
              FROM erp_rpt_pivot_view p
              LEFT JOIN erp_rpt_pivot_view_share s ON s.pivot_view_id = p.autoid
              WHERE p.report_id = @ReportId
                AND (
                  p.user_id = @UserId
                  OR (p.is_shared = TRUE AND s.target_type = 'user' AND s.target_id = @UserId)
                  OR (p.is_shared = TRUE AND s.target_type = 'role' AND s.target_id = ANY(@RoleIds))
                )
              ORDER BY p.is_last DESC, p.updated_at DESC",
            new { ReportId = reportId, UserId = userId, RoleIds = roleIds.Count > 0 ? roleIds.ToArray() : new long[] { -1 } });

        var list = pivots.ToList();

        // 如果没有任何配置，fallback 到系统缺省
        if (list.Count == 0)
        {
            var defaultPivots = await conn.QueryAsync<ReportPivotDto>(
                @"SELECT p.autoid AS Id, p.report_id AS ReportId, p.user_id AS UserId,
                         p.pivot_name AS PivotName, p.pivot_params AS PivotParams,
                         p.is_last AS IsLast, TRUE AS IsDefault,
                         p.is_shared AS IsShared, p.created_by_name AS CreatorName,
                         p.created_at AS CreatedAt, p.updated_at AS UpdatedAt
                  FROM erp_rpt_pivot_view p
                  WHERE p.report_id = @ReportId AND p.user_id = -999
                  ORDER BY p.is_last DESC, p.updated_at DESC",
                new { ReportId = reportId });
            return defaultPivots.ToList();
        }

        return list;
    }

    public async Task<long> SavePivotViewAsync(SavePivotViewRequest request, long userId, string? creatorName = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        var actualUserId = request.IsDefault ? -999 : userId;

        if (request.Id > 0)
        {
            // 更新已有配置
            await conn.ExecuteAsync(
                @"UPDATE erp_rpt_pivot_view SET pivot_name = @PivotName, pivot_params = @PivotParams,
                         is_last = @IsLast, updated_at = NOW()
                  WHERE autoid = @Id AND user_id = @UserId",
                new { request.Id, request.PivotName, request.PivotParams, request.IsLast, UserId = actualUserId });
            return request.Id.Value;
        }

        // 创建新配置
        if (request.IsLast)
        {
            // 将同用户的其他配置 is_last 置为 false
            await conn.ExecuteAsync(
                @"UPDATE erp_rpt_pivot_view SET is_last = FALSE
                  WHERE report_id = @ReportId AND user_id = @UserId",
                new { request.ReportId, UserId = actualUserId });
        }

        var id = await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO erp_rpt_pivot_view (report_id, user_id, pivot_name, pivot_params, is_last, created_by_name)
              VALUES (@ReportId, @UserId, @PivotName, @PivotParams, @IsLast, @CreatorName)
              RETURNING autoid",
            new { request.ReportId, UserId = actualUserId, request.PivotName, request.PivotParams, request.IsLast, CreatorName = creatorName ?? "" });
        return id;
    }

    public async Task<bool> DeletePivotViewAsync(long id, long userId = 0)
    {
        using var conn = _connectionFactory.CreateConnection();
        int rows;
        if (userId > 0)
        {
            // 校验所有权：只能删除自己的格式
            rows = await conn.ExecuteAsync(
                "DELETE FROM erp_rpt_pivot_view WHERE autoid = @Id AND user_id = @UserId",
                new { Id = id, UserId = userId });
        }
        else
        {
            rows = await conn.ExecuteAsync(
                "DELETE FROM erp_rpt_pivot_view WHERE autoid = @Id",
                new { Id = id });
        }
        return rows > 0;
    }

    public async Task<bool> UpdatePivotViewLastFlagAsync(long id, bool isLast)
    {
        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE erp_rpt_pivot_view SET is_last = @IsLast WHERE autoid = @Id",
            new { Id = id, IsLast = isLast });
        return rows > 0;
    }

    // ========== 透视表共享管理 ==========

    public async Task<bool> IsPivotViewOwnerAsync(long pivotViewId, long userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM erp_rpt_pivot_view WHERE autoid = @Id AND user_id = @UserId",
            new { Id = pivotViewId, UserId = userId });
        return count > 0;
    }

    public async Task<long?> GetSharePivotViewIdAsync(long shareId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<long?>(
            "SELECT pivot_view_id FROM erp_rpt_pivot_view_share WHERE autoid = @Id",
            new { Id = shareId });
    }

    public async Task<List<PivotViewShareDto>> GetSharesAsync(long pivotViewId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var shares = await conn.QueryAsync<PivotViewShareDto>(
            @"SELECT s.autoid AS Id, s.pivot_view_id AS PivotViewId,
                     s.target_type AS TargetType, s.target_id AS TargetId,
                     COALESCE(
                       CASE s.target_type
                         WHEN 'user' THEN (SELECT e.name FROM sys_users u LEFT JOIN sys_employees e ON u.employee_id = e.id WHERE u.id = s.target_id)
                         WHEN 'role' THEN (SELECT name FROM sys_roles WHERE id = s.target_id)
                       END, '未知'
                     ) AS TargetName,
                     s.created_at AS CreatedAt
              FROM erp_rpt_pivot_view_share s
              WHERE s.pivot_view_id = @PivotViewId
              ORDER BY s.created_at DESC",
            new { PivotViewId = pivotViewId });
        return shares.ToList();
    }

    public async Task<bool> AddShareAsync(PivotViewShareRequest request)
    {
        using var conn = _connectionFactory.CreateConnection();
        try
        {
            // 设置 is_shared = TRUE
            await conn.ExecuteAsync(
                "UPDATE erp_rpt_pivot_view SET is_shared = TRUE WHERE autoid = @PivotViewId",
                new { request.PivotViewId });

            await conn.ExecuteAsync(
                @"INSERT INTO erp_rpt_pivot_view_share (pivot_view_id, target_type, target_id)
                  VALUES (@PivotViewId, @TargetType, @TargetId)
                  ON CONFLICT (pivot_view_id, target_type, target_id) DO NOTHING",
                new { request.PivotViewId, request.TargetType, request.TargetId });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReportRepository] AddShareAsync 失败, PivotViewId={PivotViewId}", request.PivotViewId);
            return false;
        }
    }

    public async Task<bool> RemoveShareAsync(long shareId)
    {
        using var conn = _connectionFactory.CreateConnection();
        try
        {
            // 先获取 pivot_view_id
            var pivotViewId = await conn.ExecuteScalarAsync<long?>(
                "SELECT pivot_view_id FROM erp_rpt_pivot_view_share WHERE autoid = @Id",
                new { Id = shareId });
            if (pivotViewId == null) return false;

            await conn.ExecuteAsync(
                "DELETE FROM erp_rpt_pivot_view_share WHERE autoid = @Id",
                new { Id = shareId });

            // 检查该配置是否还有别的共享记录，如果没有则 is_shared = FALSE
            var remaining = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM erp_rpt_pivot_view_share WHERE pivot_view_id = @PivotViewId",
                new { PivotViewId = pivotViewId });
            if (remaining == 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE erp_rpt_pivot_view SET is_shared = FALSE WHERE autoid = @PivotViewId",
                    new { PivotViewId = pivotViewId });
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReportRepository] RemoveShareAsync 失败, ShareId={ShareId}", shareId);
            return false;
        }
    }

    // ========== 数据表视图配置 ==========

    public async Task<List<ReportTableViewDto>> GetTableViewsAsync(long reportId, long userId)
    {
        using var conn = _connectionFactory.CreateConnection();

        // 获取当前用户的角色ID列表
        var roleIds = (await conn.QueryAsync<long>(
            @"SELECT role_id FROM sys_user_roles WHERE user_id = @UserId",
            new { UserId = userId })).ToList();

        // 获取共享给当前用户或当前用户角色的配置，加上自己的配置
        var views = await conn.QueryAsync<ReportTableViewDto>(
            @"SELECT DISTINCT v.autoid AS Id, v.report_id AS ReportId, v.user_id AS UserId,
                     v.view_name AS ViewName, v.view_params AS ViewParams,
                     v.is_last AS IsLast, FALSE AS IsDefault,
                     v.is_shared AS IsShared,
                     v.created_by_name AS CreatorName,
                     v.created_at AS CreatedAt, v.updated_at AS UpdatedAt
              FROM erp_rpt_table_view v
              LEFT JOIN erp_rpt_table_view_share s ON s.view_id = v.autoid
              WHERE v.report_id = @ReportId
                AND (
                  v.user_id = @UserId
                  OR (v.is_shared = TRUE AND s.target_type = 'user' AND s.target_id = @UserId)
                  OR (v.is_shared = TRUE AND s.target_type = 'role' AND s.target_id = ANY(@RoleIds))
                )
              ORDER BY v.is_last DESC, v.updated_at DESC",
            new { ReportId = reportId, UserId = userId, RoleIds = roleIds.Count > 0 ? roleIds.ToArray() : new long[] { -1 } });

        var list = views.ToList();

        // 如果没有任何配置，fallback 到系统缺省
        if (list.Count == 0)
        {
            var defaultViews = await conn.QueryAsync<ReportTableViewDto>(
                @"SELECT v.autoid AS Id, v.report_id AS ReportId, v.user_id AS UserId,
                         v.view_name AS ViewName, v.view_params AS ViewParams,
                         v.is_last AS IsLast, TRUE AS IsDefault,
                         v.is_shared AS IsShared, v.created_by_name AS CreatorName,
                         v.created_at AS CreatedAt, v.updated_at AS UpdatedAt
                  FROM erp_rpt_table_view v
                  WHERE v.report_id = @ReportId AND v.user_id = -999
                  ORDER BY v.is_last DESC, v.updated_at DESC",
                new { ReportId = reportId });
            return defaultViews.ToList();
        }

        return list;
    }

    public async Task<long> SaveTableViewAsync(SaveTableViewRequest request, long userId, string? creatorName = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        var actualUserId = request.IsDefault ? -999 : userId;

        if (request.Id > 0)
        {
            // 更新已有配置
            await conn.ExecuteAsync(
                @"UPDATE erp_rpt_table_view SET view_name = @ViewName, view_params = @ViewParams,
                         is_last = @IsLast, updated_at = NOW()
                  WHERE autoid = @Id AND user_id = @UserId",
                new { request.Id, request.ViewName, request.ViewParams, request.IsLast, UserId = actualUserId });
            return request.Id.Value;
        }

        // 创建新配置
        if (request.IsLast)
        {
            // 将同用户的其他配置 is_last 置为 false
            await conn.ExecuteAsync(
                @"UPDATE erp_rpt_table_view SET is_last = FALSE
                  WHERE report_id = @ReportId AND user_id = @UserId",
                new { request.ReportId, UserId = actualUserId });
        }

        var id = await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO erp_rpt_table_view (report_id, user_id, view_name, view_params, is_last, created_by_name)
              VALUES (@ReportId, @UserId, @ViewName, @ViewParams, @IsLast, @CreatorName)
              RETURNING autoid",
            new { request.ReportId, UserId = actualUserId, request.ViewName, request.ViewParams, request.IsLast, CreatorName = creatorName ?? "" });
        return id;
    }

    public async Task<bool> DeleteTableViewAsync(long id, long userId = 0)
    {
        using var conn = _connectionFactory.CreateConnection();
        int rows;
        if (userId > 0)
        {
            // 校验所有权：只能删除自己的格式
            rows = await conn.ExecuteAsync(
                "DELETE FROM erp_rpt_table_view WHERE autoid = @Id AND user_id = @UserId",
                new { Id = id, UserId = userId });
        }
        else
        {
            rows = await conn.ExecuteAsync(
                "DELETE FROM erp_rpt_table_view WHERE autoid = @Id",
                new { Id = id });
        }
        return rows > 0;
    }

    public async Task<bool> UpdateTableViewLastFlagAsync(long id, bool isLast)
    {
        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE erp_rpt_table_view SET is_last = @IsLast WHERE autoid = @Id",
            new { Id = id, IsLast = isLast });
        return rows > 0;
    }

    // ========== 数据表视图共享管理 ==========

    public async Task<bool> IsTableViewOwnerAsync(long viewId, long userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM erp_rpt_table_view WHERE autoid = @Id AND user_id = @UserId",
            new { Id = viewId, UserId = userId });
        return count > 0;
    }

    public async Task<long?> GetTableViewShareViewIdAsync(long shareId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<long?>(
            "SELECT view_id FROM erp_rpt_table_view_share WHERE autoid = @Id",
            new { Id = shareId });
    }

    public async Task<List<TableViewShareDto>> GetTableViewSharesAsync(long viewId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var shares = await conn.QueryAsync<TableViewShareDto>(
            @"SELECT s.autoid AS Id, s.view_id AS ViewId,
                     s.target_type AS TargetType, s.target_id AS TargetId,
                     COALESCE(
                       CASE s.target_type
                         WHEN 'user' THEN (SELECT e.name FROM sys_users u LEFT JOIN sys_employees e ON u.employee_id = e.id WHERE u.id = s.target_id)
                         WHEN 'role' THEN (SELECT name FROM sys_roles WHERE id = s.target_id)
                       END, '未知'
                     ) AS TargetName,
                     s.created_at AS CreatedAt
              FROM erp_rpt_table_view_share s
              WHERE s.view_id = @ViewId
              ORDER BY s.created_at DESC",
            new { ViewId = viewId });
        return shares.ToList();
    }

    public async Task<bool> AddTableViewShareAsync(TableViewShareRequest request)
    {
        using var conn = _connectionFactory.CreateConnection();
        try
        {
            // 设置 is_shared = TRUE
            await conn.ExecuteAsync(
                "UPDATE erp_rpt_table_view SET is_shared = TRUE WHERE autoid = @ViewId",
                new { request.ViewId });

            await conn.ExecuteAsync(
                @"INSERT INTO erp_rpt_table_view_share (view_id, target_type, target_id)
                  VALUES (@ViewId, @TargetType, @TargetId)
                  ON CONFLICT (view_id, target_type, target_id) DO NOTHING",
                new { request.ViewId, request.TargetType, request.TargetId });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReportRepository] AddTableViewShareAsync 失败, ViewId={ViewId}", request.ViewId);
            return false;
        }
    }

    public async Task<bool> RemoveTableViewShareAsync(long shareId)
    {
        using var conn = _connectionFactory.CreateConnection();
        try
        {
            // 先获取 view_id
            var viewId = await conn.ExecuteScalarAsync<long?>(
                "SELECT view_id FROM erp_rpt_table_view_share WHERE autoid = @Id",
                new { Id = shareId });
            if (viewId == null) return false;

            await conn.ExecuteAsync(
                "DELETE FROM erp_rpt_table_view_share WHERE autoid = @Id",
                new { Id = shareId });

            // 检查该配置是否还有别的共享记录，如果没有则 is_shared = FALSE
            var remaining = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM erp_rpt_table_view_share WHERE view_id = @ViewId",
                new { ViewId = viewId });
            if (remaining == 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE erp_rpt_table_view SET is_shared = FALSE WHERE autoid = @ViewId",
                    new { ViewId = viewId });
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReportRepository] RemoveTableViewShareAsync 失败, ShareId={ShareId}", shareId);
            return false;
        }
    }
}