using Dapper;
using EAMS2026.Domain.DTOs.Erp;
using EAMS2026.Domain.DTOs.Erp.Report;
using EAMS2026.Domain.Interfaces.Repositories;
using EAMS2026.Infrastructure.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EAMS2026.Infrastructure.Data.Repositories.Erp;

/// <summary>
/// U8 ERP 单据修改仓储实现（Dapper + SQL Server）
/// </summary>
public class ErpVouchModifyRepository : IVouchModifyRepository
{
    private readonly ErpConnectionFactory _erpConn;
    private readonly ILogger<ErpVouchModifyRepository> _logger;

    public ErpVouchModifyRepository(ErpConnectionFactory erpConn, ILogger<ErpVouchModifyRepository> logger)
    {
        _erpConn = erpConn;
        _logger = logger;
    }

    /// <summary>
    /// 安全执行 ERP 查询，当 SQL Server 不可达或数据源未配置时返回默认值而非抛出异常
    /// </summary>
    private async Task<T?> SafeErpQueryAsync<T>(Func<global::System.Data.IDbConnection, Task<T>> query, string operation, T? fallback = default)
    {
        try
        {
            using var conn = _erpConn.CreateConnection();
            return await query(conn);
        }
        catch (DbException ex)
        {
            _logger.LogWarning(ex, "ERP SQL Server 操作失败 [{Operation}]: {Message}", operation, ex.Message);
            return fallback;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "ERP 数据源配置错误 [{Operation}]: {Message}", operation, ex.Message);
            return fallback;
        }
    }

    /// <summary>
    /// 安全执行指定年份 ERP 数据库的查询
    /// </summary>
    private async Task<T?> SafeErpQueryAsync<T>(int year, Func<global::System.Data.IDbConnection, Task<T>> query, string operation, T? fallback = default)
    {
        try
        {
            using var conn = _erpConn.CreateConnection(year);
            return await query(conn);
        }
        catch (DbException ex)
        {
            _logger.LogWarning(ex, "ERP SQL Server 操作失败 [{Operation}]: {Message}", operation, ex.Message);
            return fallback;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "ERP 数据源配置错误 [{Operation}]: {Message}", operation, ex.Message);
            return fallback;
        }
    }

    public async Task<(IEnumerable<OrderDto> Orders, int Total)> QueryOrdersAsync(VouchQueryParam param)
    {
        return (await SafeErpQueryAsync<(IEnumerable<OrderDto>, int)>(async conn =>
        {
            var (whereClause, dp) = BuildOrderWhereClause(param);

            // 获取总数
            var countSql = $@"SELECT COUNT(DISTINCT m.id) FROM SO_SOMain AS m
                              LEFT JOIN SO_SODetails AS s ON m.id = s.id
                              LEFT JOIN DispatchLists AS ds ON ds.iSOsID = s.iSOsID
                              LEFT JOIN DispatchList AS d ON d.dlid = ds.dlid
                              LEFT JOIN Customer AS c ON c.cCusCode = m.cCusCode
                              LEFT JOIN Person AS p ON p.cPersonCode = m.cPersonCode
                              WHERE 1=1 {whereClause}
                                AND 0 >= (SELECT LEN(MAX(ISNULL(cVerifier, '')))
                                          FROM DispatchList AS dm
                                          LEFT JOIN DispatchLists AS ds2 ON dm.dlid = ds2.dlid
                                          WHERE ds2.iSOsID IN (SELECT iSOsID FROM SO_SODetails WHERE id = m.id))";
            var total = await conn.ExecuteScalarAsync<int>(countSql, dp);

            // 分页查询（SQL Server 2005 兼容，使用 ROW_NUMBER）
            var dataSql = $@"SELECT * FROM (
                SELECT ROW_NUMBER() OVER(ORDER BY m.id DESC) AS RowNum,
                    m.cCusCode AS soCusCode, m.cCusName AS soCusName, m.id AS soid, m.cSOCode, m.dDate AS soDate,
                    s.cInvCode AS soInvCode, s.cInvName AS soInvName, sInv.cInvStd AS soInvStd,
                    s.iQuantity AS soQuantity,
                    d.cCusCode AS dlCusCode, d.dlid, d.cDLCode, d.dDate AS dlDate, d.cCusName AS DlCusName,
                    ds.cInvCode AS dlInvCode, ds.cInvName AS dlInvName, dInv.cInvStd AS dlInvStd,
                    ds.iQuantity AS dlQuantity, d.cVerifier AS dlVerifier,
                    p.cPersonName
                FROM SO_SOMain AS m
                LEFT JOIN SO_SODetails AS s ON m.id = s.id
                LEFT JOIN DispatchLists AS ds ON ds.iSOsID = s.iSOsID
                LEFT JOIN DispatchList AS d ON d.dlid = ds.dlid
                LEFT JOIN Inventory AS sInv ON sInv.cInvCode = s.cInvCode
                LEFT JOIN Inventory AS dInv ON dInv.cInvCode = ds.cInvCode
                LEFT JOIN Customer AS c ON c.cCusCode = m.cCusCode
                LEFT JOIN Person AS p ON p.cPersonCode = m.cPersonCode
                WHERE 1=1 {whereClause}
                  AND 0 >= (SELECT LEN(MAX(ISNULL(cVerifier, '')))
                            FROM DispatchList AS dm
                            LEFT JOIN DispatchLists AS ds2 ON dm.dlid = ds2.dlid
                            WHERE ds2.iSOsID IN (SELECT iSOsID FROM SO_SODetails WHERE id = m.id))
            ) AS T WHERE RowNum BETWEEN @StartRow AND @EndRow";

            dp.Add("StartRow", (param.Page - 1) * param.PageSize + 1);
            dp.Add("EndRow", param.Page * param.PageSize);

            var flatRows = await conn.QueryAsync<dynamic>(dataSql, dp);
            var orders = AggregateOrders(flatRows);
            return (orders, total);
        }, "QueryOrders", (Enumerable.Empty<OrderDto>(), 0)))!;
    }

    public async Task<(IEnumerable<DispatchDto> Dispatches, int Total)> QueryDispatchesAsync(VouchQueryParam param)
    {
        return (await SafeErpQueryAsync<(IEnumerable<DispatchDto>, int)>(async conn =>
        {
            var (whereClause, dp) = BuildDispatchWhereClause(param);

            var countSql = $@"SELECT COUNT(DISTINCT d.dlid) FROM DispatchList AS d
                              LEFT JOIN DispatchLists AS ds ON d.dlid = ds.dlid
                              LEFT JOIN Customer AS c ON c.cCusCode = d.cCusCode
                              LEFT JOIN Person AS p ON p.cPersonCode = d.cPersonCode
                              WHERE 1=1 {whereClause}";
            var total = await conn.ExecuteScalarAsync<int>(countSql, dp);

            var dataSql = $@"SELECT * FROM (
                SELECT ROW_NUMBER() OVER(ORDER BY d.dlid DESC) AS RowNum,
                    d.cCusCode AS dlCusCode, d.dlid, d.cDLCode, d.dDate AS dlDate,
                    d.cCusName AS DlCusName,
                    ds.cInvCode AS dlInvCode, ds.cInvName AS dlInvName, dInv.cInvStd AS dlInvStd,
                    ds.iQuantity AS dlQuantity, d.cVerifier AS dlVerifier,
                    p.cPersonName
                FROM DispatchList AS d
                LEFT JOIN DispatchLists AS ds ON d.dlid = ds.dlid
                LEFT JOIN Inventory AS dInv ON dInv.cInvCode = ds.cInvCode
                LEFT JOIN Customer AS c ON c.cCusCode = d.cCusCode
                LEFT JOIN Person AS p ON p.cPersonCode = d.cPersonCode
                WHERE 1=1 {whereClause}
            ) AS T WHERE RowNum BETWEEN @StartRow AND @EndRow";

            dp.Add("StartRow", (param.Page - 1) * param.PageSize + 1);
            dp.Add("EndRow", param.Page * param.PageSize);

            var flatRows = await conn.QueryAsync<dynamic>(dataSql, dp);

            // 详细日志：检查 Dapper 原始返回的前几条数据
            var flatList = flatRows.ToList();
            if (flatList.Count > 0)
            {
                var first = flatList.First();
                _logger.LogInformation("[QueryDispatches] Dapper原始行数: {Count}", flatList.Count);

                // 遍历 DapperRow 的列名，确认包含 DlCusName
                var columns = new List<string>();
                if (first is global::System.Dynamic.IDynamicMetaObjectProvider)
                {
                    try
                    {
                        var dict = (IDictionary<string, object?>)first;
                        columns = dict.Keys.ToList();
                    }
                    catch { }
                }
                _logger.LogInformation("[QueryDispatches] 列名列表: {Columns}", string.Join(", ", columns));

                var sample = flatList.Take(3).Select(r =>
                {
                    // 尝试不同的大小写取值
                    var name1 = "?";
                    try { name1 = (string?)r.DlCusName ?? "(null)"; } catch { name1 = "(n/a)"; }
                    var name2 = "?";
                    try { name2 = (string?)r.dlCusName ?? "(null)"; } catch { name2 = "(n/a)"; }
                    var code = "?";
                    try { code = (string?)r.cDLCode ?? "(null)"; } catch { code = "(n/a)"; }
                    return $"cDLCode=[{code}], DlCusName=[{name1}], dlCusName=[{name2}]";
                });
                _logger.LogInformation("[QueryDispatches] Dapper原始前 {Take} 条客户字段: {Samples}",
                    Math.Min(3, flatList.Count), string.Join(" | ", sample));
            }

            var dispatches = AggregateDispatches(flatList);
            return (dispatches, total);
        }, "QueryDispatches", (Enumerable.Empty<DispatchDto>(), 0)))!;
    }

    public async Task<(IEnumerable<UnverifiedDispatchRow> Rows, int Total)> QueryUnverifiedDispatchesAsync(VouchQueryParam param)
    {
        return (await SafeErpQueryAsync<(IEnumerable<UnverifiedDispatchRow>, int)>(async conn =>
        {
            var (whereClause, dp) = BuildUnverifiedDispatchWhereClause(param);

            var countSql = $@"SELECT COUNT(1) FROM DispatchList AS d
                              LEFT JOIN Customer AS c ON c.cCusCode = d.cCusCode
                              LEFT JOIN Person AS p ON p.cPersonCode = d.cPersonCode
                              WHERE (d.cVerifier IS NULL OR d.cVerifier = '') {whereClause}";
            var total = await conn.ExecuteScalarAsync<int>(countSql, dp);

            var dataSql = $@"SELECT * FROM (
                SELECT ROW_NUMBER() OVER(ORDER BY d.dDate DESC) AS RowNum,
                    d.dlid, d.cDLCode, d.cCusCode AS CusCode, d.cCusName AS CusName,
                    d.dDate AS DDate, d.cVerifier, p.cPersonName
                FROM DispatchList AS d
                LEFT JOIN Customer AS c ON c.cCusCode = d.cCusCode
                LEFT JOIN Person AS p ON p.cPersonCode = d.cPersonCode
                WHERE (d.cVerifier IS NULL OR d.cVerifier = '') {whereClause}
            ) AS T WHERE RowNum BETWEEN @StartRow AND @EndRow";

            dp.Add("StartRow", (param.Page - 1) * param.PageSize + 1);
            dp.Add("EndRow", param.Page * param.PageSize);

            var rows = await conn.QueryAsync<UnverifiedDispatchRow>(dataSql, dp);
            return (rows, total);
        }, "QueryUnverifiedDispatches", (Enumerable.Empty<UnverifiedDispatchRow>(), 0)))!;
    }

    public async Task<DispatchDto?> GetDispatchByCodeAsync(string dlcode)
    {
        return await SafeErpQueryAsync<DispatchDto?>(async conn =>
        {
            var sql = @"SELECT d.cCusCode AS dlCusCode, d.dlid, d.cDLCode, d.dDate AS dlDate, d.cCusName AS DlCusName,
                        ds.cInvCode AS dlInvCode, ds.cInvName AS dlInvName, dInv.cInvStd AS dlInvStd,
                        ds.iQuantity AS dlQuantity, d.cVerifier AS dlVerifier
                        FROM DispatchList AS d
                        LEFT JOIN DispatchLists AS ds ON d.dlid = ds.dlid
                        LEFT JOIN Inventory AS dInv ON dInv.cInvCode = ds.cInvCode
                        WHERE d.cDLCode = @DlCode";
            var flatRows = await conn.QueryAsync<dynamic>(sql, new { DlCode = dlcode });
            var dispatches = AggregateDispatches(flatRows);
            return dispatches.FirstOrDefault()!;
        }, "GetDispatchByCode", null);
    }

    public async Task<bool> UpdateOrderCustomerAsync(int soid, string newCusCode, string newCusName)
    {
        return await SafeErpQueryAsync<bool>(async conn =>
        {
            var sql = @"UPDATE SO_SOMain SET cCusCode = @NewCusCode, cCusName = @NewCusName
                        WHERE id = @Soid";
            var rows = await conn.ExecuteAsync(sql, new { NewCusCode = newCusCode, NewCusName = newCusName, Soid = soid });
            return rows > 0;
        }, "UpdateOrderCustomer", false);
    }

    public async Task<bool> UpdateDispatchCustomerAsync(int dlid, string newCusCode, string newCusName)
    {
        return await SafeErpQueryAsync<bool>(async conn =>
        {
            var sql = @"UPDATE DispatchList SET cCusCode = @NewCusCode, cCusName = @NewCusName
                        WHERE dlid = @Dlid AND (cVerifier IS NULL OR cVerifier = '')";
            var rows = await conn.ExecuteAsync(sql, new { NewCusCode = newCusCode, NewCusName = newCusName, Dlid = dlid });
            return rows > 0;
        }, "UpdateDispatchCustomer", false);
    }

    public async Task<bool> UpdateDispatchDateAsync(int dlid, DateTime newDate)
    {
        return await SafeErpQueryAsync<bool>(async conn =>
        {
            var sql = @"UPDATE DispatchList SET dDate = @NewDate
                        WHERE dlid = @Dlid AND (cVerifier IS NULL OR cVerifier = '')";
            var rows = await conn.ExecuteAsync(sql, new { NewDate = newDate, Dlid = dlid });
            return rows > 0;
        }, "UpdateDispatchDate", false);
    }

    public async Task<CustomerRefDto?> GetCustomerReferenceAsync(string code)
    {
        return await SafeErpQueryAsync<CustomerRefDto?>(async conn =>
        {
            var sql = "SELECT cCusCode AS CusCode, cCusName AS CusName FROM Customer WHERE cCusCode = @Code";
            return await conn.QueryFirstOrDefaultAsync<CustomerRefDto>(sql, new { Code = code });
        }, "GetCustomerReference", null);
    }

    public async Task<bool> IsDispatchUnverifiedAsync(int dlid)
    {
        return await SafeErpQueryAsync<bool>(async conn =>
        {
            var sql = "SELECT COUNT(1) FROM DispatchList WHERE dlid = @Dlid AND (cVerifier IS NULL OR cVerifier = '')";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Dlid = dlid });
            return count > 0;
        }, "IsDispatchUnverified", false);
    }

    /// <summary>
    /// 检查销售订单是否有关联的已审核发货单
    /// </summary>
    public async Task<IEnumerable<int>> GetDispatchIdsByOrderIdAsync(int soid)
    {
        return await SafeErpQueryAsync<IEnumerable<int>>(async conn =>
        {
            var sql = @"SELECT DISTINCT d.dlid FROM DispatchList AS d
                        INNER JOIN DispatchLists AS ds ON d.dlid = ds.dlid
                        INNER JOIN SO_SODetails AS s ON s.iSOsID = ds.iSOsID
                        WHERE s.id = @Soid";
            var results = await conn.QueryAsync<int>(sql, new { Soid = soid });
            _logger.LogInformation("[GetDispatchIdsByOrderId] 订单 {Soid} 关联发货单: {Dlids}", soid, string.Join(",", results));
            return results;
        }, "GetDispatchIdsByOrderId", Enumerable.Empty<int>()) ?? Enumerable.Empty<int>();
    }

    public async Task<bool> HasVerifiedDispatchesAsync(int soid)
    {
        return await SafeErpQueryAsync<bool>(async conn =>
        {
            var sql = @"SELECT COUNT(1) FROM DispatchList AS d
                        INNER JOIN DispatchLists AS ds ON d.dlid = ds.dlid
                        INNER JOIN SO_SODetails AS s ON s.iSOsID = ds.iSOsID
                        WHERE s.id = @Soid AND (d.cVerifier IS NOT NULL AND d.cVerifier != '')";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Soid = soid });
            return count > 0;
        }, "HasVerifiedDispatches", false);
    }

    // ===================== 私有方法 =====================

    private (string WhereClause, DynamicParameters Params) BuildOrderWhereClause(VouchQueryParam param)
    {
        var dp = new DynamicParameters();
        var sb = new StringBuilder();

        AddCondition(sb, dp, "m.cCusCode = @CusCode", "CusCode", param.CusCode);
        AddCondition(sb, dp, "m.cCusName LIKE @CusName", "CusName", param.CusName != null ? $"%{param.CusName}%" : null);
        AddCondition(sb, dp, "m.cCusName LIKE @CusAbbName", "CusAbbName", param.CusAbbName != null ? $"%{param.CusAbbName}%" : null);
        AddCondition(sb, dp, "c.cCusPerson = @CusContact", "CusContact", param.CusContact);
        AddCondition(sb, dp, "m.cPersonCode = @CusPPerson", "CusPPerson", param.CusPPerson);
        AddCondition(sb, dp, "(c.cCusPhone LIKE @CusPhone OR c.cCusHand LIKE @CusPhone OR c.cCusFax LIKE @CusPhone)", "CusPhone", param.CusPhone != null ? $"%{param.CusPhone}%" : null);
        AddCondition(sb, dp, "(c.cCusPhone LIKE @CusMobile OR c.cCusHand LIKE @CusMobile)", "CusMobile", param.CusMobile != null ? $"%{param.CusMobile}%" : null);
        AddCondition(sb, dp, "m.cCusOAddress LIKE @CusAddr", "CusAddr", param.CusAddr != null ? $"%{param.CusAddr}%" : null);
        AddCondition(sb, dp, "m.cSOCode LIKE @VouchCode", "VouchCode", param.VouchCode != null ? $"%{param.VouchCode}%" : null);
        AddCondition(sb, dp, "m.dDate >= @VouchDateFrom", "VouchDateFrom", param.VouchDateFrom);
        AddCondition(sb, dp, "m.dDate <= @VouchDateTo", "VouchDateTo", param.VouchDateTo);

        return (sb.ToString(), dp);
    }

    private (string WhereClause, DynamicParameters Params) BuildDispatchWhereClause(VouchQueryParam param)
    {
        var dp = new DynamicParameters();
        var sb = new StringBuilder();

        AddCondition(sb, dp, "d.cCusCode = @CusCode", "CusCode", param.CusCode);
        AddCondition(sb, dp, "d.cCusName LIKE @CusName", "CusName", param.CusName != null ? $"%{param.CusName}%" : null);
        AddCondition(sb, dp, "d.cCusName LIKE @CusAbbName", "CusAbbName", param.CusAbbName != null ? $"%{param.CusAbbName}%" : null);
        AddCondition(sb, dp, "d.cPersonCode = @CusPPerson", "CusPPerson", param.CusPPerson);
        AddCondition(sb, dp, "d.cDLCode LIKE @VouchCode", "VouchCode", param.VouchCode != null ? $"%{param.VouchCode}%" : null);
        AddCondition(sb, dp, "d.dDate >= @VouchDateFrom", "VouchDateFrom", param.VouchDateFrom);
        AddCondition(sb, dp, "d.dDate <= @VouchDateTo", "VouchDateTo", param.VouchDateTo);

        // 审核状态过滤
        if (!string.IsNullOrEmpty(param.VerifierStatus))
        {
            if (param.VerifierStatus == "unverified")
                sb.Append(" AND (d.cVerifier IS NULL OR d.cVerifier = '')");
            else if (param.VerifierStatus == "verified")
                sb.Append(" AND (d.cVerifier IS NOT NULL AND d.cVerifier != '')");
        }

        return (sb.ToString(), dp);
    }

    private (string WhereClause, DynamicParameters Params) BuildUnverifiedDispatchWhereClause(VouchQueryParam param)
    {
        var dp = new DynamicParameters();
        var sb = new StringBuilder();

        AddCondition(sb, dp, "d.cCusCode LIKE @CusCode", "CusCode", param.CusCode != null ? $"%{param.CusCode}%" : null);
        AddCondition(sb, dp, "d.cCusName LIKE @CusName", "CusName", param.CusName != null ? $"%{param.CusName}%" : null);
        AddCondition(sb, dp, "d.cDLCode LIKE @VouchCode", "VouchCode", param.VouchCode != null ? $"%{param.VouchCode}%" : null);
        AddCondition(sb, dp, "d.cPersonCode = @CusPPerson", "CusPPerson", param.CusPPerson);
        AddCondition(sb, dp, "d.dDate >= @VouchDateFrom", "VouchDateFrom", param.VouchDateFrom);
        AddCondition(sb, dp, "d.dDate <= @VouchDateTo", "VouchDateTo", param.VouchDateTo);

        return (sb.ToString(), dp);
    }

    private static void AddCondition(StringBuilder sb, DynamicParameters dp, string clause, string paramName, object? value)
    {
        if (value == null) return;
        if (value is string s && string.IsNullOrWhiteSpace(s)) return;
        sb.Append(" AND ").Append(clause);
        dp.Add(paramName, value);
    }

    /// <summary>
    /// 将扁平查询结果聚合为 OrderDto 层级结构
    /// </summary>
    private static List<OrderDto> AggregateOrders(IEnumerable<dynamic> rows)
    {
        var orderMap = new Dictionary<int, OrderDto>();
        var orderDetailSet = new Dictionary<string, object>();
        var dispatchDetailSet = new Dictionary<string, object>();

        foreach (var row in rows)
        {
            int soid = (int)row.soid;
            if (!orderMap.TryGetValue(soid, out var order))
            {
                order = new OrderDto
                {
                    Soid = soid,
                    CSOCode = (string?)row.cSOCode ?? "",
                    SoDate = row.soDate as DateTime?,
                    SoCusCode = (string?)row.soCusCode ?? "",
                    SoCusName = (string?)row.soCusName ?? "",
                    CPersonName = row.cPersonName as string
                };
                orderMap[soid] = order;
            }

            // 订单明细去重
            var soInvCode = (string?)row.soInvCode;
            if (!string.IsNullOrEmpty(soInvCode))
            {
                var detailKey = $"so_{soid}_{soInvCode}_{row.soQuantity}";
                if (!orderDetailSet.ContainsKey(detailKey))
                {
                    orderDetailSet[detailKey] = null!;
                    order.OrderDetails.Add(new OrderDetailDto
                    {
                        SoInvCode = soInvCode,
                        SoInvName = (string?)row.soInvName,
                        SoInvStd = (string?)row.soInvStd,
                        SoQuantity = (decimal)row.soQuantity
                    });
                }
            }

            // 发货单
            if (row.dlid != null)
            {
                int dlid = (int)row.dlid;
                var dispatch = order.Dispatches.FirstOrDefault(d => d.Dlid == dlid);
                if (dispatch == null)
                {
                    dispatch = new DispatchDto
                    {
                        Dlid = dlid,
                        CDLCode = (string?)row.cDLCode ?? "",
                        DlDate = row.dlDate as DateTime?,
                        DlCusCode = (string?)row.dlCusCode ?? "",
                        DlCusName = (string?)row.DlCusName ?? "",
                        CVerifier = row.dlVerifier as string
                    };
                    order.Dispatches.Add(dispatch);
                }

                // 发货明细去重
                var dlInvCode = (string?)row.dlInvCode;
                if (!string.IsNullOrEmpty(dlInvCode))
                {
                    var detailKey = $"dl_{dlid}_{dlInvCode}_{row.dlQuantity}";
                    if (!dispatchDetailSet.ContainsKey(detailKey))
                    {
                        dispatchDetailSet[detailKey] = null!;
                        dispatch.DispatchDetails.Add(new DispatchDetailDto
                        {
                            DlInvCode = dlInvCode,
                            DlInvName = (string?)row.dlInvName,
                            DlInvStd = (string?)row.dlInvStd,
                            DlQuantity = (decimal)row.dlQuantity
                        });
                    }
                }
            }
        }

        return orderMap.Values.ToList();
    }

    /// <summary>
    /// 将扁平查询结果聚合为 DispatchDto 层级结构
    /// </summary>
    private static List<DispatchDto> AggregateDispatches(IEnumerable<dynamic> rows)
    {
        var dispatchMap = new Dictionary<int, DispatchDto>();
        var detailSet = new Dictionary<string, object>();

        foreach (var row in rows)
        {
            if (row.dlid == null) continue;
            int dlid = (int)row.dlid;
            if (!dispatchMap.TryGetValue(dlid, out var dispatch))
            {
                dispatch = new DispatchDto
                {
                    Dlid = dlid,
                    CDLCode = (string?)row.cDLCode ?? "",
                    DlDate = row.dlDate as DateTime?,
                    DlCusCode = (string?)row.dlCusCode ?? "",
                    DlCusName = (string?)row.DlCusName ?? "",
                    CVerifier = row.dlVerifier as string
                };
                dispatchMap[dlid] = dispatch;
            }

            var dlInvCode = (string?)row.dlInvCode;
            if (!string.IsNullOrEmpty(dlInvCode))
            {
                var key = $"dl_{dlid}_{dlInvCode}_{row.dlQuantity}";
                if (!detailSet.ContainsKey(key))
                {
                    detailSet[key] = null!;
                    dispatch.DispatchDetails.Add(new DispatchDetailDto
                    {
                        DlInvCode = dlInvCode,
                        DlInvName = (string?)row.dlInvName,
                        DlInvStd = (string?)row.dlInvStd,
                        DlQuantity = (decimal)row.dlQuantity
                    });
                }
            }
        }

        return dispatchMap.Values.ToList();
    }

    public async Task<List<SalespersonDto>> GetSalespersonsAsync()
    {
        var currentYear = DateTime.Today.Year;
        return await SafeErpQueryAsync(currentYear, async conn =>
        {
            var rows = await conn.QueryAsync<(string Code, string Name)>(
                @"SELECT p.cPersonCode, p.cPersonName
                  FROM Person p
                  INNER JOIN Department d ON d.cDepCode = p.cDepCode
                  WHERE d.cDepName LIKE N'%业务%'
                  ORDER BY p.cPersonCode");
            return rows.Select(r => new SalespersonDto { Code = r.Code, Name = r.Name }).ToList();
        }, "GetSalespersons", new List<SalespersonDto>()) ?? new List<SalespersonDto>();
    }
}