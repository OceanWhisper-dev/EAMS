using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.Erp;
using EAMS2026.Domain.Common;
using EAMS2026.Domain.DTOs.Erp;
using EAMS2026.Domain.DTOs.Erp.Report;
using EAMS2026.Domain.Entities.Erp;
using EAMS2026.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.Erp;

public class VouchModifyService : IVouchModifyService
{
    private readonly IVouchModifyRepository _vouchRepo;
    private readonly IVouchModifyLogRepository _logRepo;
    private readonly ILogger<VouchModifyService> _logger;

    public VouchModifyService(
        IVouchModifyRepository vouchRepo,
        IVouchModifyLogRepository logRepo,
        ILogger<VouchModifyService> logger)
    {
        _vouchRepo = vouchRepo;
        _logRepo = logRepo;
        _logger = logger;
    }

    // ==================== 查询 ====================

    public async Task<PagedResult<OrderDto>> QueryOrdersAsync(VouchQueryParam param)
    {
        _logger.LogInformation("[QueryOrders] 开始查询订单, 客户编码={CusCode}, 日期范围={DateFrom}~{DateTo}",
            param.CusCode, param.VouchDateFrom?.ToString("yyyy-MM-dd"), param.VouchDateTo?.ToString("yyyy-MM-dd"));
        var (orders, total) = await _vouchRepo.QueryOrdersAsync(param);
        _logger.LogInformation("[QueryOrders] 查询完成, 共 {Count} 条 (总计 {Total})", orders.Count(), total);
        return new PagedResult<OrderDto>(orders, total, param.Page, param.PageSize);
    }

    public async Task<PagedResult<DispatchDto>> QueryDispatchesAsync(VouchQueryParam param)
    {
        _logger.LogInformation("[QueryDispatches] 开始查询发货单, 客户编码={CusCode}, 日期范围={DateFrom}~{DateTo}",
            param.CusCode, param.VouchDateFrom?.ToString("yyyy-MM-dd"), param.VouchDateTo?.ToString("yyyy-MM-dd"));
        var (dispatches, total) = await _vouchRepo.QueryDispatchesAsync(param);
        var dispatchList = dispatches.ToList();
        _logger.LogInformation("[QueryDispatches] 查询完成, 共 {Count} 条 (总计 {Total})", dispatchList.Count, total);

        // 详细日志：检查前几条的客户信息
        if (dispatchList.Count > 0)
        {
            var sample = dispatchList.Take(3).Select(d =>
                $"dlid={d.Dlid}, cDLCode={d.CDLCode}, dlCusCode={d.DlCusCode}, dlCusName=[{d.DlCusName}]");
            _logger.LogInformation("[QueryDispatches] 前 {Take} 条客户信息: {Samples}",
                Math.Min(3, dispatchList.Count), string.Join(" | ", sample));
        }
        else
        {
            _logger.LogWarning("[QueryDispatches] 查询结果为空，total={Total}", total);
        }

        return new PagedResult<DispatchDto>(dispatchList, total, param.Page, param.PageSize);
    }

    public async Task<PagedResult<UnverifiedDispatchRow>> QueryUnverifiedDispatchesAsync(VouchQueryParam param)
    {
        _logger.LogInformation("[QueryUnverifiedDispatches] 开始查询未审核发货单, 客户编码={CusCode}, 日期范围={DateFrom}~{DateTo}",
            param.CusCode, param.VouchDateFrom?.ToString("yyyy-MM-dd"), param.VouchDateTo?.ToString("yyyy-MM-dd"));
        var (rows, total) = await _vouchRepo.QueryUnverifiedDispatchesAsync(param);
        _logger.LogInformation("[QueryUnverifiedDispatches] 查询完成, 共 {Count} 条 (总计 {Total})", rows.Count(), total);
        return new PagedResult<UnverifiedDispatchRow>(rows, total, param.Page, param.PageSize);
    }

    public async Task<DispatchDto?> GetDispatchByCodeAsync(string dlcode)
    {
        _logger.LogInformation("[GetDispatchByCode] 查询发货单: dlcode={DlCode}", dlcode);
        var result = await _vouchRepo.GetDispatchByCodeAsync(dlcode);
        _logger.LogInformation("[GetDispatchByCode] 查询结果: {Found}", result != null ? "找到" : "未找到");
        return result;
    }

    public async Task<bool> HasVerifiedDispatchesAsync(int soid)
    {
        _logger.LogInformation("[HasVerifiedDispatches] 检查订单 {Soid} 是否有已审核发货单", soid);
        var result = await _vouchRepo.HasVerifiedDispatchesAsync(soid);
        _logger.LogInformation("[HasVerifiedDispatches] 订单 {Soid} 检查结果: {Result}", soid, result);
        return result;
    }

    // ==================== 修改订单客户 ====================

    public async Task<VouchModifyResult> UpdateOrderCustomerAsync(UpdateCustomerRequest request, long operatorId, string operatorName)
    {
        _logger.LogInformation("[UpdateOrderCustomer] 操作人={Op}({OpId}), soid={Soid}, 新客户={NewCusCode}/{NewCusName}",
            operatorName, operatorId, request.Soid, request.NewCusCode, request.NewCusName);

        if (!request.Soid.HasValue)
            return VouchModifyResult.Fail("订单ID不能为空");

        if (string.IsNullOrWhiteSpace(request.NewCusCode))
            return VouchModifyResult.Fail("新客户编码不能为空");

        // 校验客户存在
        _logger.LogInformation("[UpdateOrderCustomer] 校验客户编码 {Code} 是否存在", request.NewCusCode);
        var customer = await _vouchRepo.GetCustomerReferenceAsync(request.NewCusCode);
        if (customer == null)
        {
            _logger.LogWarning("[UpdateOrderCustomer] 客户编码 {Code} 不存在", request.NewCusCode);
            return VouchModifyResult.Fail($"客户编码 {request.NewCusCode} 不存在");
        }

        // 执行修改
        _logger.LogInformation("[UpdateOrderCustomer] 执行修改 soid={Soid}", request.Soid);
        var success = await _vouchRepo.UpdateOrderCustomerAsync(request.Soid.Value, request.NewCusCode, request.NewCusName);
        if (!success)
        {
            _logger.LogWarning("[UpdateOrderCustomer] 修改失败 soid={Soid}", request.Soid);
            return VouchModifyResult.Fail("修改订单客户失败，请检查单据状态");
        }

        // 同步修改关联发货单
        if (request.SyncDispatches)
        {
            _logger.LogInformation("[UpdateOrderCustomer] 开始同步修改关联发货单客户: soid={Soid}", request.Soid);
            var dispatchIds = await _vouchRepo.GetDispatchIdsByOrderIdAsync(request.Soid.Value);
            var dispatchList = dispatchIds.ToList();
            _logger.LogInformation("[UpdateOrderCustomer] 订单 {Soid} 关联了 {Count} 个发货单: {Dlids}",
                request.Soid, dispatchList.Count, string.Join(",", dispatchList));

            int successCount = 0, skipCount = 0, failCount = 0;
            foreach (var dlid in dispatchList)
            {
                _logger.LogInformation("[UpdateOrderCustomer] 检查发货单 {Dlid} 是否未审核", dlid);
                var isUnverified = await _vouchRepo.IsDispatchUnverifiedAsync(dlid);
                if (!isUnverified)
                {
                    _logger.LogWarning("[UpdateOrderCustomer] 发货单 {Dlid} 已审核，跳过更新", dlid);
                    skipCount++;
                    continue;
                }

                _logger.LogInformation("[UpdateOrderCustomer] 更新发货单 {Dlid} 客户: {NewCusCode}/{NewCusName}",
                    dlid, request.NewCusCode, request.NewCusName);
                var dispatchSuccess = await _vouchRepo.UpdateDispatchCustomerAsync(dlid, request.NewCusCode, request.NewCusName);
                if (dispatchSuccess)
                {
                    successCount++;
                    _logger.LogInformation("[UpdateOrderCustomer] 发货单 {Dlid} 客户更新成功", dlid);

                    await _logRepo.AddAsync(new VouchModifyLog
                    {
                        VouchType = "dispatch",
                        VouchId = dlid,
                        VouchCode = dlid.ToString(),
                        FieldName = "cCusCode",
                        OldValue = request.OldCusCode,
                        NewValue = request.NewCusCode,
                        OperatorId = operatorId,
                        OperatorName = operatorName,
                        Status = "SUCCESS"
                    });
                }
                else
                {
                    failCount++;
                    _logger.LogWarning("[UpdateOrderCustomer] 发货单 {Dlid} 客户更新失败", dlid);
                }
            }

            _logger.LogInformation("[UpdateOrderCustomer] 同步发货单完成: 成功 {SuccessCount}, 跳过 {SkipCount}, 失败 {FailCount} (共 {Total})",
                successCount, skipCount, failCount, dispatchList.Count);
        }

        // 记录日志
        await _logRepo.AddAsync(new VouchModifyLog
        {
            VouchType = "order",
            VouchId = request.Soid.Value,
            VouchCode = request.Soid.Value.ToString(),
            FieldName = "cCusCode",
            OldValue = request.OldCusCode,
            NewValue = request.NewCusCode,
            OperatorId = operatorId,
            OperatorName = operatorName,
            Status = "SUCCESS"
        });

        _logger.LogInformation("[UpdateOrderCustomer] 修改成功 soid={Soid}", request.Soid);
        return VouchModifyResult.Ok("订单客户修改成功");
    }

    // ==================== 修改发货单客户 ====================

    public async Task<VouchModifyResult> UpdateDispatchCustomerAsync(UpdateCustomerRequest request, long operatorId, string operatorName)
    {
        _logger.LogInformation("[UpdateDispatchCustomer] 操作人={Op}({OpId}), dlid={Dlid}, 新客户={NewCusCode}/{NewCusName}",
            operatorName, operatorId, request.Dlid, request.NewCusCode, request.NewCusName);

        if (!request.Dlid.HasValue)
            return VouchModifyResult.Fail("发货单ID不能为空");

        if (string.IsNullOrWhiteSpace(request.NewCusCode))
            return VouchModifyResult.Fail("新客户编码不能为空");

        _logger.LogInformation("[UpdateDispatchCustomer] 校验客户编码 {Code} 是否存在", request.NewCusCode);
        var customer = await _vouchRepo.GetCustomerReferenceAsync(request.NewCusCode);
        if (customer == null)
        {
            _logger.LogWarning("[UpdateDispatchCustomer] 客户编码 {Code} 不存在", request.NewCusCode);
            return VouchModifyResult.Fail($"客户编码 {request.NewCusCode} 不存在");
        }

        // 检查未审核
        _logger.LogInformation("[UpdateDispatchCustomer] 检查发货单 {Dlid} 是否未审核", request.Dlid);
        var isUnverified = await _vouchRepo.IsDispatchUnverifiedAsync(request.Dlid.Value);
        if (!isUnverified)
        {
            _logger.LogWarning("[UpdateDispatchCustomer] 发货单 {Dlid} 已审核，不可修改", request.Dlid);
            return VouchModifyResult.Fail("该发货单已审核，不可修改");
        }

        _logger.LogInformation("[UpdateDispatchCustomer] 执行修改 dlid={Dlid}", request.Dlid);
        var success = await _vouchRepo.UpdateDispatchCustomerAsync(request.Dlid.Value, request.NewCusCode, request.NewCusName);
        if (!success)
        {
            _logger.LogWarning("[UpdateDispatchCustomer] 修改发货单客户失败 dlid={Dlid}", request.Dlid);
            return VouchModifyResult.Fail("修改发货单客户失败");
        }

        await _logRepo.AddAsync(new VouchModifyLog
        {
            VouchType = "dispatch",
            VouchId = request.Dlid.Value,
            VouchCode = request.Dlid.Value.ToString(),
            FieldName = "cCusCode",
            OldValue = request.OldCusCode,
            NewValue = request.NewCusCode,
            OperatorId = operatorId,
            OperatorName = operatorName,
            Status = "SUCCESS"
        });

        _logger.LogInformation("[UpdateDispatchCustomer] 修改成功 dlid={Dlid}", request.Dlid);
        return VouchModifyResult.Ok("发货单客户修改成功");
    }

    // ==================== 修改发货单日期（单笔） ====================

    public async Task<VouchModifyResult> UpdateDispatchDateAsync(UpdateDateRequest request, long operatorId, string operatorName)
    {
        _logger.LogInformation("[UpdateDispatchDate] 操作人={Op}({OpId}), dlid={Dlid}, 新日期={NewDate}",
            operatorName, operatorId, request.Dlid, request.NewDate.ToString("yyyy-MM-dd"));

        if (!request.Dlid.HasValue)
            return VouchModifyResult.Fail("发货单ID不能为空");

        var isUnverified = await _vouchRepo.IsDispatchUnverifiedAsync(request.Dlid.Value);
        if (!isUnverified)
            return VouchModifyResult.Fail("该发货单已审核，不可修改");

        var success = await _vouchRepo.UpdateDispatchDateAsync(request.Dlid.Value, request.NewDate);
        if (!success)
            return VouchModifyResult.Fail("修改发货日期失败");

        await _logRepo.AddAsync(new VouchModifyLog
        {
            VouchType = "dispatch",
            VouchId = request.Dlid.Value,
            VouchCode = request.DlCode ?? request.Dlid.Value.ToString(),
            FieldName = "dDate",
            OldValue = null,
            NewValue = request.NewDate.ToString("yyyy-MM-dd"),
            OperatorId = operatorId,
            OperatorName = operatorName,
            Status = "SUCCESS"
        });

        _logger.LogInformation("[UpdateDispatchDate] 修改成功 dlid={Dlid}, newDate={NewDate}",
            request.Dlid, request.NewDate.ToString("yyyy-MM-dd"));
        return VouchModifyResult.Ok("发货日期修改成功");
    }

    // ==================== 批量修改发货单日期 ====================

    public async Task<VouchModifyBatchResult> BatchUpdateDispatchDateAsync(BatchUpdateDateRequest request, long operatorId, string operatorName)
    {
        _logger.LogInformation("[BatchUpdateDispatchDate] 操作人={Op}({OpId}), 共 {Count} 条, 自动计算={AutoCalc}, 目标日期={NewDate}",
            operatorName, operatorId, request.Dlids.Count, request.AutoCalculate, request.NewDate.ToString("yyyy-MM-dd"));

        var result = new VouchModifyBatchResult { TotalCount = request.Dlids.Count };

        if (request.AutoCalculate)
        {
            request.NewDate = CalculateFirstSundayOfNextMonth();
            _logger.LogInformation("[BatchUpdateDispatchDate] 自动计算目标日期为: {NewDate}", request.NewDate.ToString("yyyy-MM-dd"));
        }

        foreach (var dlid in request.Dlids)
        {
            try
            {
                var isUnverified = await _vouchRepo.IsDispatchUnverifiedAsync(dlid);
                if (!isUnverified)
                {
                    _logger.LogWarning("[BatchUpdateDispatchDate] 单据 {Dlid} 已审核，跳过", dlid);
                    result.Failures.Add(new BatchFailItem { Dlid = dlid, DlCode = "", ErrorMessage = "单据已审核" });
                    continue;
                }

                var success = await _vouchRepo.UpdateDispatchDateAsync(dlid, request.NewDate);
                if (success)
                {
                    result.SuccessCount++;
                    await _logRepo.AddAsync(new VouchModifyLog
                    {
                        VouchType = "dispatch",
                        VouchId = dlid,
                        VouchCode = dlid.ToString(),
                        FieldName = "dDate",
                        OldValue = null,
                        NewValue = request.NewDate.ToString("yyyy-MM-dd"),
                        OperatorId = operatorId,
                        OperatorName = operatorName,
                        Status = "SUCCESS"
                    });
                }
                else
                {
                    _logger.LogWarning("[BatchUpdateDispatchDate] 单据 {Dlid} 更新失败", dlid);
                    result.Failures.Add(new BatchFailItem { Dlid = dlid, DlCode = "", ErrorMessage = "更新失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BatchUpdateDispatchDate] 单据 {Dlid} 异常", dlid);
                result.Failures.Add(new BatchFailItem { Dlid = dlid, DlCode = "", ErrorMessage = ex.Message });
            }
        }

        result.FailCount = result.TotalCount - result.SuccessCount;
        _logger.LogInformation("[BatchUpdateDispatchDate] 批量修改完成: 成功 {Success}/{Total} 条",
            result.SuccessCount, result.TotalCount);
        return result;
    }

    // ==================== 批量修改发货单客户 ====================

    public async Task<VouchModifyBatchResult> BatchUpdateDispatchCustomerAsync(BatchUpdateCustomerRequest request, long operatorId, string operatorName)
    {
        _logger.LogInformation("[BatchUpdateDispatchCustomer] 操作人={Op}({OpId}), 共 {Count} 条, 新客户={NewCusCode}/{NewCusName}",
            operatorName, operatorId, request.Dlids.Count, request.NewCusCode, request.NewCusName);

        var result = new VouchModifyBatchResult { TotalCount = request.Dlids.Count };

        // 校验客户是否存在
        var customer = await _vouchRepo.GetCustomerReferenceAsync(request.NewCusCode);
        if (customer == null)
        {
            _logger.LogWarning("[BatchUpdateDispatchCustomer] 客户编码 {Code} 不存在", request.NewCusCode);
            result.Failures.Add(new BatchFailItem { Dlid = 0, DlCode = "", ErrorMessage = $"客户编码 {request.NewCusCode} 不存在" });
            result.FailCount = result.TotalCount;
            return result;
        }

        foreach (var dlid in request.Dlids)
        {
            try
            {
                var isUnverified = await _vouchRepo.IsDispatchUnverifiedAsync(dlid);
                if (!isUnverified)
                {
                    _logger.LogWarning("[BatchUpdateDispatchCustomer] 单据 {Dlid} 已审核，跳过", dlid);
                    result.Failures.Add(new BatchFailItem { Dlid = dlid, DlCode = "", ErrorMessage = "单据已审核" });
                    continue;
                }

                var success = await _vouchRepo.UpdateDispatchCustomerAsync(dlid, request.NewCusCode, request.NewCusName);
                if (success)
                {
                    result.SuccessCount++;
                    await _logRepo.AddAsync(new VouchModifyLog
                    {
                        VouchType = "dispatch",
                        VouchId = dlid,
                        VouchCode = dlid.ToString(),
                        FieldName = "cCusCode",
                        OldValue = request.OldCusCode,
                        NewValue = request.NewCusCode,
                        OperatorId = operatorId,
                        OperatorName = operatorName,
                        Status = "SUCCESS"
                    });
                }
                else
                {
                    _logger.LogWarning("[BatchUpdateDispatchCustomer] 单据 {Dlid} 更新失败", dlid);
                    result.Failures.Add(new BatchFailItem { Dlid = dlid, DlCode = "", ErrorMessage = "更新失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BatchUpdateDispatchCustomer] 单据 {Dlid} 异常", dlid);
                result.Failures.Add(new BatchFailItem { Dlid = dlid, DlCode = "", ErrorMessage = ex.Message });
            }
        }

        result.FailCount = result.TotalCount - result.SuccessCount;
        _logger.LogInformation("[BatchUpdateDispatchCustomer] 批量修改完成: 成功 {Success}/{Total} 条",
            result.SuccessCount, result.TotalCount);
        return result;
    }

    // ==================== 客户参照 ====================

    public async Task<CustomerRefDto?> GetCustomerReferenceAsync(string code)
    {
        _logger.LogInformation("[GetCustomerReference] 查询客户参照: code={Code}", code);
        var result = await _vouchRepo.GetCustomerReferenceAsync(code);
        _logger.LogInformation("[GetCustomerReference] 查询结果: {Found}", result != null ? $"找到客户 {result.CusName}" : "未找到");
        return result;
    }

    // ==================== 日志查询 ====================

    public Task<PagedResult<VouchModifyLog>> QueryLogsAsync(string? vouchType, long? operatorId,
        DateTime? from, DateTime? to, int page, int pageSize)
        => _logRepo.QueryAsync(vouchType, operatorId, from, to, page, pageSize);

    // ==================== 私有方法 ====================

    /// <summary>
    /// 计算下月第一个周日
    /// </summary>
    private static DateTime CalculateFirstSundayOfNextMonth()
    {
        var today = DateTime.Today;
        var firstOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
        var dayOfWeek = (int)firstOfNextMonth.DayOfWeek; // 0=Sunday
        var daysToAdd = dayOfWeek == 0 ? 0 : 7 - dayOfWeek;
        return firstOfNextMonth.AddDays(daysToAdd);
    }

    // ==================== 业务员 ====================

    /// <summary>
    /// 获取业务员列表
    /// 优先从本地 erp_settings_salesperson_map 映射表读取（共享报表模块的业务员映射数据），
    /// 无映射数据时回退到查询 U8 ERP Person 表
    /// </summary>
    public async Task<List<SalespersonDto>> GetSalespersonsAsync()
    {
        _logger.LogInformation("[GetSalespersons] 从 erp_settings_salesperson_map 读取业务员列表");
        try
        {
            var list = await _logRepo.GetMappedSalespersonsAsync();
            _logger.LogInformation("[GetSalespersons] 从映射表获取到 {Count} 个业务员", list.Count);

            if (list.Count == 0)
            {
                _logger.LogInformation("[GetSalespersons] 映射表无数据，回退到 U8 ERP 查询");
                return await _vouchRepo.GetSalespersonsAsync();
            }
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetSalespersons] 读取映射表失败，回退到 U8 ERP 查询");
            return await _vouchRepo.GetSalespersonsAsync();
        }
    }
}