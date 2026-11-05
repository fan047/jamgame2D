using System.Text.RegularExpressions;

namespace Holedown
{
    // Chinese presentation only. Config keys, stable IDs and diagnostic data stay unchanged.
    public static class ChineseText
    {
        public static string Session(SessionPhase phase) => phase switch
        {
            SessionPhase.Initializing => "准备中",
            SessionPhase.Aiming => "瞄准中",
            SessionPhase.Volley => "发射中",
            SessionPhase.Recalling => "召回中",
            SessionPhase.Settling => "结算中",
            SessionPhase.Won => "已通关",
            SessionPhase.Lost => "挑战失败",
            _ => "准备中"
        };
        public static string Configuration(string label) => (label ?? "Not loaded")
            .Replace("Not loaded", "尚未加载").Replace("Built-in", "内置配置")
            .Replace("S:", "球参：").Replace("L:", "关卡：");

        public static string Error(string detail)
        {
            if (string.IsNullOrEmpty(detail)) return "";
            var location = Regex.Match(detail, @"(?:settings|level)\.json(?:\.[^:\s]+)*").Value;
            if (location.Length == 0) location = "配置";
            if (detail.Contains("tested stage"))
            {
                var id = Regex.Match(detail, "tested stage '([^']*)'").Groups[1].Value;
                return $"关卡标识“{id}”已不存在，请恢复该关卡或返回选关。";
            }
            if (detail.Contains("manual balls")) return "手填球数必须是 1～2147483647 的整数。";
            if (detail.Contains("entry balls")) return "入关球数必须是 1～2147483647 的整数。";
            if (detail.Contains("stage.id") || detail.Contains("Select a valid stage")) return "请先加载配置并选择有效关卡。";
            if (detail.Contains("ball mode")) return "球数方式无效，请重新选择。";
            string reason;
            if (detail.Contains("Missing:") || detail.Contains("required file is missing"))
            {
                var missing = Regex.Match(detail, @"Missing:.*?((?:settings|level)\.json)");
                if (missing.Success) location = missing.Groups[1].Value;
                reason = "文件缺失，请同时放入 settings.json 与 level.json。";
            }
            else if (detail.Contains("line ") || detail.Contains("position "))
            {
                var line = Regex.Match(detail, @"line (\d+), position (\d+)");
                reason = "JSON 格式错误，请检查括号、逗号、引号及重复字段。";
                if (line.Success) reason += $" 定位：第 {line.Groups[1].Value} 行，第 {line.Groups[2].Value} 列。";
            }
            else if (detail.Contains("supported versions")) reason = "配置格式版本不受支持，请使用配套工具导出。";
            else if (detail.Contains("unknown field")) reason = "未知字段，请检查字段名称。";
            else if (detail.Contains("1-32 letters")) reason = "需为 1～32 个英文字母、数字、点、下划线或连字符。";
            else if (detail.Contains("1-40 characters")) reason = "关名需为 1～40 个字符。";
            else if (detail.Contains("missing or duplicate stage id")) reason = "关卡标识缺失或重复。";
            else if (detail.Contains("final stage cannot")) reason = "最终关不能包含奖励块。";
            else if (detail.Contains("Int32 maximum")) reason = "奖励合计或累计球数超过 2147483647，请降低数值。";
            else if (detail.Contains("exactly one target")) reason = "最后一条补行必须且只能有一个目标块。";
            else if (detail.Contains("target must be in")) reason = "目标块必须位于最后一条补行。";
            else if (detail.Contains("mutually exclusive")) reason = "目标块不能同时作为奖励块。";
            else if (detail.Contains("overlaps another block")) reason = "方块与其他方块占格重叠。";
            else if (detail.Contains("height=2 requires")) reason = "竖块只允许用于初始棋盘，宽度必须为 1；补行高度必须为 1。";
            else if (detail.Contains("exceeds 8 columns")) reason = "方块超出八列棋盘。";
            else if (detail.Contains("exceeds 8 rows")) reason = "方块超出八行棋盘。";
            else if (detail.Contains("row 0")) reason = "补行中的方块行号必须为 0。";
            else if (detail.Contains("expected 0-7")) reason = "需为 0～7 的整数。";
            else if (detail.Contains("expected 1 or 2")) reason = "需为 1 或 2。";
            else if (detail.Contains("non-negative Int32")) reason = "需为 0～2147483647 的整数。";
            else if (detail.Contains("expected positive integer")) reason = "需为 1～2147483647 的整数。";
            else if (detail.Contains("32-bit integer")) reason = "需为合法的 32 位整数。";
            else if (detail.Contains("expected true or false")) reason = "需为布尔值 true 或 false。";
            else if (detail.Contains("positive finite"))
            {
                reason = "需为有限正数。";
                var maximum = Regex.Match(detail, @"below ([\d.]+)");
                if (maximum.Success) reason += $" 数值需小于 {maximum.Groups[1].Value.TrimEnd('.')}。";
            }
            else if (detail.Contains("expected number")) reason = "需填写数值。";
            else if (detail.Contains("expected array") || detail.Contains("missing blocks array")) reason = "需填写方块或补行数组。";
            else if (detail.Contains("expected object")) reason = "需填写配置对象。";
            else if (detail.Contains("expected string")) reason = "需填写文本。";
            else if (detail.Contains("at least one stage")) reason = "至少需要一个关卡。";
            else if (detail.Contains("at least one incomingRows")) reason = "需有初始棋盘和至少一条补行。";
            else if (detail.Contains("missing block")) reason = "缺少方块数据。";
            else reason = "读取失败，请检查文件权限、JSON 格式与字段值。";
            return location + "：" + reason;
        }
    }
}
