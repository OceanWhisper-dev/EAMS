using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EAMS2026.Api.JsonConverters;

/// <summary>
/// 支持将空字符串 "" 反序列化为 null 的 DateOnly? JSON 转换器。
/// 解决前端 el-date-picker 未选日期时发送空字符串导致的 400 错误。
/// </summary>
public class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrEmpty(str))
                return null;

            if (DateOnly.TryParseExact(str, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;

            if (DateOnly.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return date;
        }

        // 回退到默认反序列化（会抛出异常）
        return JsonSerializer.Deserialize<DateOnly?>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString(Format));
        else
            writer.WriteNullValue();
    }
}