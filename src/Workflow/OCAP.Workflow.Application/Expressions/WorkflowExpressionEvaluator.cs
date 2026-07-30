using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OCAP.Workflow.Application.Expressions;

public class WorkflowExpressionEvaluator : Abstractions.IWorkflowExpressionEvaluator
{
    private static readonly Regex InterpolationPattern = new(@"\{\{(.+?)\}\}", RegexOptions.Compiled);

    public bool EvaluateBool(string expression, IDictionary<string, object> variables)
    {
        var result = Evaluate(expression, variables);
        return ToBool(result);
    }

    public object? Evaluate(string expression, IDictionary<string, object> variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;

        var parser = new ExpressionParser(expression.Trim(), variables);
        return parser.ParseExpression();
    }

    public string Interpolate(string template, IDictionary<string, object> variables)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return InterpolationPattern.Replace(template, match =>
        {
            var path = match.Groups[1].Value.Trim();
            var value = ResolvePath(path, variables);
            return FormatValue(value) ?? match.Value;
        });
    }

    internal static object? ResolvePath(string path, IDictionary<string, object> variables)
    {
        if (variables.TryGetValue(path, out var direct))
            return Unwrap(direct);

        var segments = ParsePathSegments(path);
        if (segments.Count == 0)
            return null;

        if (!variables.TryGetValue(segments[0].Name, out var current))
            return null;

        current = Unwrap(current);
        for (var i = 1; i < segments.Count; i++)
        {
            if (current == null)
                return null;

            var segment = segments[i];
            if (segment.IsIndex)
            {
                current = GetIndexedValue(current, segment.Name);
            }
            else
            {
                current = GetPropertyValue(current, segment.Name);
            }
        }

        return current;
    }

    private static List<PathSegment> ParsePathSegments(string path)
    {
        var segments = new List<PathSegment>();
        var i = 0;
        while (i < path.Length)
        {
            if (path[i] == '.')
            {
                i++;
                continue;
            }

            if (path[i] == '[')
            {
                var close = path.IndexOf(']', i);
                if (close < 0)
                    break;
                var indexStr = path[(i + 1)..close].Trim();
                segments.Add(new PathSegment(indexStr, true));
                i = close + 1;
                continue;
            }

            var start = i;
            while (i < path.Length && path[i] != '.' && path[i] != '[')
                i++;

            segments.Add(new PathSegment(path[start..i], false));
        }

        return segments;
    }

    private static object? GetPropertyValue(object current, string name)
    {
        current = Unwrap(current)!;

        if (current is JsonElement json && json.ValueKind == JsonValueKind.Object)
        {
            if (json.TryGetProperty(name, out var prop))
                return Unwrap(prop);
            return null;
        }

        if (current is IDictionary<string, object> dict)
        {
            return dict.TryGetValue(name, out var val) ? Unwrap(val) : null;
        }

        if (current is IReadOnlyDictionary<string, object> roDict)
        {
            return roDict.TryGetValue(name, out var val) ? Unwrap(val) : null;
        }

        return null;
    }

    private static object? GetIndexedValue(object current, string indexStr)
    {
        current = Unwrap(current)!;

        if (int.TryParse(indexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            if (current is JsonElement json && json.ValueKind == JsonValueKind.Array)
            {
                if (index >= 0 && index < json.GetArrayLength())
                    return Unwrap(json[index]);
                return null;
            }

            if (current is IList<object> list)
            {
                return index >= 0 && index < list.Count ? Unwrap(list[index]) : null;
            }

            if (current is Array arr)
            {
                return index >= 0 && index < arr.Length ? Unwrap(arr.GetValue(index)) : null;
            }
        }

        return GetPropertyValue(current, indexStr);
    }

    internal static object? Unwrap(object? value)
    {
        if (value is JsonElement elem)
        {
            return elem.ValueKind switch
            {
                JsonValueKind.String => elem.GetString(),
                JsonValueKind.Number => elem.TryGetInt64(out var l) ? l : elem.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => elem,
                JsonValueKind.Object => elem,
                _ => elem.GetRawText()
            };
        }

        return value;
    }

    internal static string? FormatValue(object? value)
    {
        value = Unwrap(value);
        return value switch
        {
            null => string.Empty,
            bool b => b.ToString().ToLowerInvariant(),
            string s => s,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    internal static bool ToBool(object? value)
    {
        value = Unwrap(value);
        return value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrEmpty(s) && s != "false" && s != "0",
            int i => i != 0,
            long l => l != 0,
            double d => d != 0,
            _ => true
        };
    }

    private sealed record PathSegment(string Name, bool IsIndex);

    private sealed class ExpressionParser
    {
        private readonly string _input;
        private readonly IDictionary<string, object> _variables;
        private int _pos;

        public ExpressionParser(string input, IDictionary<string, object> variables)
        {
            _input = input;
            _variables = variables;
        }

        public object? ParseExpression() => ParseOr();

        private object? ParseOr()
        {
            var left = ParseAnd();
            SkipWhitespace();
            while (MatchKeyword("||"))
            {
                var right = ParseAnd();
                left = ToBool(left) || ToBool(right);
            }
            return left;
        }

        private object? ParseAnd()
        {
            var left = ParseEquality();
            SkipWhitespace();
            while (MatchKeyword("&&"))
            {
                var right = ParseEquality();
                left = ToBool(left) && ToBool(right);
            }
            return left;
        }

        private object? ParseEquality()
        {
            var left = ParseComparison();
            SkipWhitespace();
            while (true)
            {
                if (MatchOperator("=="))
                {
                    var right = ParseComparison();
                    left = AreEqual(left, right);
                }
                else if (MatchOperator("!="))
                {
                    var right = ParseComparison();
                    left = !AreEqual(left, right);
                }
                else break;
            }
            return left;
        }

        private object? ParseComparison()
        {
            var left = ParseUnary();
            SkipWhitespace();
            while (true)
            {
                if (MatchOperator(">="))
                {
                    var right = ParseUnary();
                    left = Compare(left, right) >= 0;
                }
                else if (MatchOperator("<="))
                {
                    var right = ParseUnary();
                    left = Compare(left, right) <= 0;
                }
                else if (MatchOperator(">"))
                {
                    var right = ParseUnary();
                    left = Compare(left, right) > 0;
                }
                else if (MatchOperator("<"))
                {
                    var right = ParseUnary();
                    left = Compare(left, right) < 0;
                }
                else break;
            }
            return left;
        }

        private object? ParseUnary()
        {
            SkipWhitespace();
            if (MatchOperator("!"))
            {
                return !ToBool(ParseUnary());
            }
            return ParsePostfix();
        }

        private object? ParsePostfix()
        {
            var value = ParsePrimary();
            SkipWhitespace();
            while (Peek() == '(')
            {
                value = ParseFunctionCall(value?.ToString() ?? string.Empty);
            }
            return value;
        }

        private object? ParseFunctionCall(string name)
        {
            Expect('(');
            var args = new List<object?>();
            SkipWhitespace();
            if (Peek() != ')')
            {
                do
                {
                    args.Add(ParseOr());
                    SkipWhitespace();
                } while (MatchChar(','));
            }
            Expect(')');
            return EvaluateFunction(name, args);
        }

        private object? EvaluateFunction(string name, List<object?> args)
        {
            return name.ToLowerInvariant() switch
            {
                "contains" => Contains(args),
                "startswith" => StartsWith(args),
                "isempty" => IsEmpty(args),
                "length" => Length(args),
                _ => throw new InvalidOperationException($"Función desconocida: {name}")
            };
        }

        private static object Contains(List<object?> args)
        {
            if (args.Count < 2) return false;
            var haystack = FormatValue(args[0]) ?? string.Empty;
            var needle = FormatValue(args[1]) ?? string.Empty;
            return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
        }

        private static object StartsWith(List<object?> args)
        {
            if (args.Count < 2) return false;
            var text = FormatValue(args[0]) ?? string.Empty;
            var prefix = FormatValue(args[1]) ?? string.Empty;
            return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static object IsEmpty(List<object?> args)
        {
            if (args.Count == 0) return true;
            var val = Unwrap(args[0]);
            return val switch
            {
                null => true,
                string s => string.IsNullOrEmpty(s),
                JsonElement { ValueKind: JsonValueKind.Array } arr => arr.GetArrayLength() == 0,
                ICollection<object> col => col.Count == 0,
                _ => false
            };
        }

        private static object Length(List<object?> args)
        {
            if (args.Count == 0) return 0;
            var val = Unwrap(args[0]);
            return val switch
            {
                string s => s.Length,
                JsonElement { ValueKind: JsonValueKind.Array } arr => arr.GetArrayLength(),
                JsonElement { ValueKind: JsonValueKind.String } str => str.GetString()?.Length ?? 0,
                ICollection<object> col => col.Count,
                _ => FormatValue(val)?.Length ?? 0
            };
        }

        private object? ParsePrimary()
        {
            SkipWhitespace();
            if (MatchKeyword("true")) return true;
            if (MatchKeyword("false")) return false;
            if (MatchKeyword("null")) return null;

            if (Peek() == '"')
                return ParseString();

            if (Peek() == '\'')
                return ParseCharString();

            if (char.IsDigit(Peek()) || (Peek() == '-' && char.IsDigit(Peek(1))))
                return ParseNumber();

            if (Peek() == '(')
            {
                Expect('(');
                var expr = ParseOr();
                Expect(')');
                return expr;
            }

            return ParseIdentifierOrPath();
        }

        private string ParseString()
        {
            Expect('"');
            var start = _pos;
            while (_pos < _input.Length && _input[_pos] != '"')
            {
                if (_input[_pos] == '\\') _pos++;
                _pos++;
            }
            var str = _input[start.._pos];
            Expect('"');
            return str;
        }

        private string ParseCharString()
        {
            Expect('\'');
            var start = _pos;
            while (_pos < _input.Length && _input[_pos] != '\'')
            {
                if (_input[_pos] == '\\') _pos++;
                _pos++;
            }
            var str = _input[start.._pos];
            Expect('\'');
            return str;
        }

        private object ParseNumber()
        {
            var start = _pos;
            if (Peek() == '-') _pos++;
            while (_pos < _input.Length && (char.IsDigit(_input[_pos]) || _input[_pos] == '.'))
                _pos++;
            var numStr = _input[start.._pos];
            if (numStr.Contains('.'))
                return double.Parse(numStr, CultureInfo.InvariantCulture);
            return long.Parse(numStr, CultureInfo.InvariantCulture);
        }

        private object? ParseIdentifierOrPath()
        {
            var start = _pos;
            while (_pos < _input.Length && (char.IsLetterOrDigit(_input[_pos]) || _input[_pos] == '_' || _input[_pos] == '.' || _input[_pos] == '['))
            {
                if (_input[_pos] == '[')
                {
                    _pos++;
                    while (_pos < _input.Length && _input[_pos] != ']') _pos++;
                }
                _pos++;
            }

            var token = _input[start.._pos];
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Token inesperado en expresión.");

            if (IsFunctionNameFollowedByParen(token))
                return token;

            return ResolvePath(token, _variables);
        }

        private bool IsFunctionNameFollowedByParen(string token)
        {
            SkipWhitespace();
            return char.IsLetter(token[0]) && !token.Contains('.') && !token.Contains('[') && Peek() == '(';
        }

        private static bool AreEqual(object? left, object? right)
        {
            left = Unwrap(left);
            right = Unwrap(right);
            if (left == null && right == null) return true;
            if (left == null || right == null) return false;
            if (left is string ls && right is string rs) return string.Equals(ls, rs, StringComparison.Ordinal);
            if (IsNumeric(left) && IsNumeric(right))
                return Compare(left, right) == 0;
            return left.Equals(right);
        }

        private static bool IsNumeric(object value) =>
            value is int or long or float or double or decimal;

        private static int Compare(object? left, object? right)
        {
            left = Unwrap(left);
            right = Unwrap(right);
            if (left == null && right == null) return 0;
            if (left == null) return -1;
            if (right == null) return 1;

            var ld = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            var rd = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            return ld.CompareTo(rd);
        }

        private char Peek(int offset = 0) =>
            _pos + offset < _input.Length ? _input[_pos + offset] : '\0';

        private void SkipWhitespace()
        {
            while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
                _pos++;
        }

        private bool MatchChar(char c)
        {
            SkipWhitespace();
            if (Peek() != c) return false;
            _pos++;
            return true;
        }

        private bool MatchOperator(string op)
        {
            SkipWhitespace();
            if (!_input.AsSpan(_pos).StartsWith(op)) return false;
            _pos += op.Length;
            return true;
        }

        private bool MatchKeyword(string keyword)
        {
            SkipWhitespace();
            if (_pos + keyword.Length > _input.Length) return false;
            var slice = _input.AsSpan(_pos, keyword.Length);
            if (!slice.Equals(keyword, StringComparison.Ordinal)) return false;
            if (_pos + keyword.Length < _input.Length)
            {
                var next = _input[_pos + keyword.Length];
                if (char.IsLetterOrDigit(next) || next == '_') return false;
            }
            _pos += keyword.Length;
            return true;
        }

        private void Expect(char c)
        {
            SkipWhitespace();
            if (Peek() != c)
                throw new InvalidOperationException($"Se esperaba '{c}' en posición {_pos}.");
            _pos++;
        }
    }
}
