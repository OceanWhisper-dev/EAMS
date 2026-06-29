using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.Attendance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EAMS2026.Infrastructure.Data;

public class DingTalkService : IDingTalkService
{
    private readonly HttpClient _httpClient;
    private readonly DingTalkOptions _options;
    private readonly ILogger<DingTalkService> _logger;
    private string? _cachedToken;
    private DateTime _tokenExpireTime = DateTime.MinValue;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const long DINGTALK_TICK_OFFSET = 621356256000000000;

    public DingTalkService(HttpClient httpClient, IOptions<DingTalkOptions> options, ILogger<DingTalkService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.Now < _tokenExpireTime)
            return _cachedToken;

        var url = $"/gettoken?appkey={_options.AppKey}&appsecret={_options.AppSecret}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TokenApiResponse>(json, _jsonOptions);

        if (result?.Errcode != 0 || string.IsNullOrEmpty(result?.AccessToken))
            throw new InvalidOperationException($"获取钉钉Token失败: {result?.Errmsg ?? json}");

        _cachedToken = result.AccessToken;
        _tokenExpireTime = DateTime.Now.AddSeconds(_options.TokenExpireBufferSeconds);
        _logger.LogInformation("钉钉Token已刷新");

        return _cachedToken;
    }

    public async Task<List<DingTalkDepartmentDto>> GetDepartmentsAsync()
    {
        var token = await GetTokenAsync();
        var url = $"/department/list?access_token={token}";
        var response = await _httpClient.GetAsync(url);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DepartmentListApiResponse>(json, _jsonOptions);

        if (result?.Errcode != 0 || result?.Department == null)
            throw new InvalidOperationException($"获取钉钉部门列表失败: {result?.Errmsg ?? json}");

        return result.Department
            .Select(d => new DingTalkDepartmentDto { Id = d.Id, Name = d.Name })
            .ToList();
    }

    public async Task<List<DingTalkUserDto>> GetDepartmentUsersAsync(long departmentId)
    {
        var token = await GetTokenAsync();
        var url = $"/user/simplelist?access_token={token}&department_id={departmentId}&offset=0&size={_options.DepartmentUserPageSize}";
        var response = await _httpClient.GetAsync(url);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UserSimpleListApiResponse>(json, _jsonOptions);

        if (result?.Errcode != 0 || result?.Userlist == null)
            throw new InvalidOperationException($"获取钉钉部门用户失败(dept={departmentId}): {result?.Errmsg ?? json}");

        return result.Userlist
            .Select(u => new DingTalkUserDto { UserId = u.Userid, Name = u.Name, DepartmentId = departmentId })
            .ToList();
    }

    public async Task<List<DingTalkUserDto>> GetAttendanceGroupUsersAsync()
    {
        var allUsers = new List<DingTalkUserDto>();

        var departments = await GetDepartmentsAsync();
        var atteGroup = await GetAttendanceGroupAsync();

        var atteDepartments = departments.Where(d => atteGroup.DeptNameList.Contains(d.Name)).ToList();

        foreach (var dept in atteDepartments)
        {
            var users = await GetDepartmentUsersAsync(dept.Id);
            allUsers.AddRange(users);
        }

        return allUsers;
    }

    public async Task<List<DingTalkAttendanceRecordDto>> GetAttendanceRecordsAsync(
        DateTime startDate, DateTime endDate, List<string> userIdList)
    {
        var records = new List<DingTalkAttendanceRecordDto>();
        var token = await GetTokenAsync();

        int sectionCount = (int)Math.Ceiling(userIdList.Count / (double)_options.AttendancePageSize);

        for (int i = 0; i < sectionCount; i++)
        {
            var chunk = userIdList.Skip(i * _options.AttendancePageSize).Take(_options.AttendancePageSize).ToList();

            var currentStart = startDate;
            while (currentStart < endDate)
            {
                var batchEnd = currentStart.AddDays(_options.AttendanceBatchDays);
                if (batchEnd > endDate) batchEnd = endDate;

                int offset = 0;
                bool hasMore = true;

                while (hasMore)
                {
                    var body = new AttendanceListRequest
                    {
                        UserIdList = chunk,
                        WorkDateFrom = currentStart.ToString("yyyy-MM-dd HH:mm:ss"),
                        WorkDateTo = batchEnd.ToString("yyyy-MM-dd HH:mm:ss"),
                        Offset = offset,
                        Limit = _options.AttendancePageSize
                    };

                    var requestJson = JsonSerializer.Serialize(body, _jsonOptions);
                    var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

                    var url = $"/attendance/list?access_token={token}";

                    // 限流重试：最多重试 MaxRetries 次，指数退避
                    DingTalkAttendanceListResponse? result = null;
                    string responseJson = "";
                    int retryCount = 0;
                    int maxRetries = _options.MaxRetries;

                    while (retryCount <= maxRetries)
                    {
                        // 每次重试需要重新创建请求体内容
                        var retryContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
                        var httpResponse = await _httpClient.PostAsync(url, retryContent);
                        responseJson = await httpResponse.Content.ReadAsStringAsync();
                        result = JsonSerializer.Deserialize<DingTalkAttendanceListResponse>(responseJson, _jsonOptions);

                        if (result?.Errcode == 0)
                            break;

                        bool isRateLimited = result?.Errmsg != null &&
                            (result.Errmsg.Contains("qps", StringComparison.OrdinalIgnoreCase) ||
                             result.Errmsg.Contains("流控", StringComparison.OrdinalIgnoreCase) ||
                             result.Errmsg.Contains("too many", StringComparison.OrdinalIgnoreCase) ||
                             result.Errmsg.Contains("frequency", StringComparison.OrdinalIgnoreCase));

                        if (!isRateLimited || retryCount >= maxRetries)
                        {
                            _logger.LogWarning("钉钉考勤记录API调用异常(offset={Offset}): {Msg}",
                                offset, result?.Errmsg ?? responseJson);
                            break;
                        }

                        retryCount++;
                        int delayMs = _options.RetryBaseDelayMs * (int)Math.Pow(2, retryCount - 1);
                        _logger.LogInformation("钉钉API限流，第{Retry}次重试，等待{Delay}ms", retryCount, delayMs);
                        await Task.Delay(delayMs);
                    }

                    if (result?.Errcode == 0 && result.Recordresult != null)
                    {
                        foreach (var r in result.Recordresult)
                        {
                            if (r.TimeResult != "NotSigned")
                            {
                                records.Add(new DingTalkAttendanceRecordDto
                                {
                                    UserId = r.UserId,
                                    UserName = "",
                                    WorkDate = ConvertDingTalkTime(r.WorkDate),
                                    UserCheckTime = ConvertDingTalkTime(r.UserCheckTime),
                                    CheckType = r.CheckType ?? "",
                                    TimeResult = r.TimeResult ?? "",
                                    BaseCheckTime = r.BaseCheckTime.HasValue ? ConvertDingTalkTime(r.BaseCheckTime.Value) : null
                                });
                            }
                        }

                        hasMore = result.HasMore;
                        offset += _options.AttendancePageSize;
                    }
                    else if (retryCount > maxRetries || result?.Errcode != 0)
                    {
                        break;
                    }
                }

                currentStart = batchEnd.AddDays(1);
            }
        }

        return records;
    }

    public DateTime ConvertDingTalkTime(string dingTalkTimestamp)
    {
        if (long.TryParse(dingTalkTimestamp, out long tick))
        {
            return new DateTime(tick * 10000 + DINGTALK_TICK_OFFSET);
        }
        return DateTime.MinValue;
    }

    public DateTime ConvertDingTalkTime(long dingTalkTimestamp)
    {
        return new DateTime(dingTalkTimestamp * 10000 + DINGTALK_TICK_OFFSET);
    }

    private async Task<AtGroupResponse> GetAttendanceGroupAsync()
    {
        var token = await GetTokenAsync();
        var url = $"/topapi/attendance/getsimplegroups?access_token={token}";

        var requestBody = new { };
        var requestJson = JsonSerializer.Serialize(requestBody, _jsonOptions);
        var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<DingTalkApiResponse<GroupResult>>(responseJson, _jsonOptions);

        if (result?.Errcode != 0 || result?.Result == null)
            throw new InvalidOperationException($"获取钉钉考勤组失败: {result?.Errmsg ?? responseJson}");

        var group = result.Result.Groups.FirstOrDefault(g => g.GroupName == _options.AttendanceGroupName);
        if (group == null)
            throw new InvalidOperationException($"未找到考勤组 '{_options.AttendanceGroupName}'");

        return group;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private class TokenApiResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }
        [JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private class DepartmentListApiResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }
        [JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }
        [JsonPropertyName("department")]
        public List<DepartmentItem>? Department { get; set; }
    }

    private class DepartmentItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class UserSimpleListApiResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }
        [JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }
        [JsonPropertyName("userlist")]
        public List<UserItem>? Userlist { get; set; }
    }

    private class UserItem
    {
        public string Userid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private class GroupResult
    {
        public List<AtGroupResponse> Groups { get; set; } = new();
    }

    private class AtGroupResponse
    {
        public string GroupName { get; set; } = string.Empty;
        public List<string> DeptNameList { get; set; } = new();
    }

    private class AttendanceListRequest
    {
        [JsonPropertyName("userIdList")]
        public List<string> UserIdList { get; set; } = new();
        [JsonPropertyName("workDateFrom")]
        public string WorkDateFrom { get; set; } = string.Empty;
        [JsonPropertyName("workDateTo")]
        public string WorkDateTo { get; set; } = string.Empty;
        [JsonPropertyName("offset")]
        public int Offset { get; set; }
        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }

    private class DingTalkAttendanceListResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }
        [JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }
        [JsonPropertyName("hasMore")]
        public bool HasMore { get; set; }
        [JsonPropertyName("recordresult")]
        public List<AttendanceRecordItem>? Recordresult { get; set; }
    }

    private class AttendanceRecordItem
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;
        [JsonPropertyName("workDate")]
        public long WorkDate { get; set; }
        [JsonPropertyName("userCheckTime")]
        public long UserCheckTime { get; set; }
        [JsonPropertyName("checkType")]
        public string? CheckType { get; set; }
        [JsonPropertyName("timeResult")]
        public string? TimeResult { get; set; }
        [JsonPropertyName("baseCheckTime")]
        public long? BaseCheckTime { get; set; }
    }
}

public class DingTalkOptions
{
    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string AttendanceGroupName { get; set; } = "考勤组";

    /// <summary>获取部门用户列表的分页大小</summary>
    public int DepartmentUserPageSize { get; set; } = 100;

    /// <summary>考勤记录 API 每页/每批大小</summary>
    public int AttendancePageSize { get; set; } = 50;

    /// <summary>API 限流最大重试次数</summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>限流重试指数退避基数（毫秒）</summary>
    public int RetryBaseDelayMs { get; set; } = 1000;

    /// <summary>Token 缓存过期时间（秒），官方为 7200 秒</summary>
    public int TokenExpireBufferSeconds { get; set; } = 7000;

    /// <summary>查询考勤记录时每批次的天数</summary>
    public int AttendanceBatchDays { get; set; } = 7;
}

public class DingTalkApiResponse<T>
{
    [JsonPropertyName("errcode")]
    public int Errcode { get; set; }
    [JsonPropertyName("errmsg")]
    public string? Errmsg { get; set; }
    public T? Result { get; set; }
}