using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Paging
{
    /// <summary>
    /// Parses a filter expression string into a <see cref="FilterNode"/> tree.
    /// Grammar (precedence: <c>||</c> looser than <c>&amp;&amp;</c>, parentheses override):
    /// <code>
    /// expr      := or
    /// or        := and ( "||" and )*
    /// and       := primary ( "&amp;&amp;" primary )*
    /// primary   := "(" expr ")" | condition
    /// condition := identifier operator value
    /// operator  := "==" | "!=" | "&gt;" | "&gt;=" | "&lt;" | "&lt;=" | "contains" | "startswith" | "endswith" | "in"
    ///            | "!contains" | "!startswith" | "!endswith" | "!in"
    /// value     := string | number | "true" | "false" | "null" | "[" (value ("," value)*)? "]"
    /// </code>
    /// Throws <see cref="FormatException"/> on syntax errors.
    /// </summary>
    internal static class FilterExpressionParser
    {
        [return: NotNullIfNotNull(nameof(expression))]
        internal static FilterNode? Parse(string? expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return null;
            }

            var tokens = Lexer.Tokenize(expression!);
            var parser = new Parser(tokens);
            var node = parser.ParseExpression();
            parser.Expect(TokenType.End);
            return node;
        }

        private enum TokenType
        {
            Identifier,
            String,
            Number,
            Equal,
            NotEqual,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual,
            And,
            Or,
            LParen,
            RParen,
            LBracket,
            RBracket,
            Comma,
            End,
        }

        private readonly struct Token
        {
            internal Token(TokenType type, string text, int position)
            {
                this.Type = type;
                this.Text = text;
                this.Position = position;
            }

            internal TokenType Type { get; }

            internal string Text { get; }

            internal int Position { get; }
        }

        private sealed class Parser
        {
            private readonly IReadOnlyList<Token> tokens;
            private int index;

            internal Parser(IReadOnlyList<Token> tokens)
            {
                this.tokens = tokens;
            }

            private Token Current => this.tokens[this.index];

            internal FilterNode ParseExpression()
            {
                return this.ParseOr();
            }

            internal void Expect(TokenType type)
            {
                if (this.Current.Type != type)
                {
                    throw Error($"Expected {type} but found '{this.Current.Text}'", this.Current);
                }
            }

            private FilterNode ParseOr()
            {
                var nodes = new List<FilterNode> { this.ParseAnd() };
                while (this.Current.Type == TokenType.Or)
                {
                    this.index++;
                    nodes.Add(this.ParseAnd());
                }

                return nodes.Count == 1 ? nodes[0] : new FilterGroup(FilterLogic.Or, nodes.ToArray());
            }

            private FilterNode ParseAnd()
            {
                var nodes = new List<FilterNode> { this.ParsePrimary() };
                while (this.Current.Type == TokenType.And)
                {
                    this.index++;
                    nodes.Add(this.ParsePrimary());
                }

                return nodes.Count == 1 ? nodes[0] : new FilterGroup(FilterLogic.And, nodes.ToArray());
            }

            private FilterNode ParsePrimary()
            {
                if (this.Current.Type == TokenType.LParen)
                {
                    this.index++;
                    var node = this.ParseExpression();
                    this.Expect(TokenType.RParen);
                    this.index++;
                    return node;
                }

                return this.ParseCondition();
            }

            private FilterNode ParseCondition()
            {
                if (this.Current.Type != TokenType.Identifier || FilterOperatorTokens.IsKeyword(this.Current.Text))
                {
                    throw Error($"Expected a property name but found '{this.Current.Text}'", this.Current);
                }

                var property = this.Current.Text;
                this.index++;

                var filterOperator = this.ParseOperator();
                var value = this.ParseValue();

                if ((filterOperator == FilterOperator.In || filterOperator == FilterOperator.NotIn) && value is not object?[])
                {
                    throw Error("The 'in' operator requires a list value, e.g. Id in [1, 2, 3].", this.Current);
                }

                return new FilterCondition(property, filterOperator, value);
            }

            private FilterOperator ParseOperator()
            {
                var token = this.Current;
                switch (token.Type)
                {
                    case TokenType.Equal:
                        this.index++;
                        return FilterOperator.Equal;
                    case TokenType.NotEqual:
                        this.index++;
                        return FilterOperator.NotEqual;
                    case TokenType.GreaterThan:
                        this.index++;
                        return FilterOperator.GreaterThan;
                    case TokenType.GreaterThanOrEqual:
                        this.index++;
                        return FilterOperator.GreaterThanOrEqual;
                    case TokenType.LessThan:
                        this.index++;
                        return FilterOperator.LessThan;
                    case TokenType.LessThanOrEqual:
                        this.index++;
                        return FilterOperator.LessThanOrEqual;
                    case TokenType.Identifier when FilterOperatorTokens.TryParseKeyword(token.Text, out var keywordOperator):
                        this.index++;
                        return keywordOperator;
                    default:
                        throw Error($"Expected a filter operator but found '{token.Text}'", token);
                }
            }

            private object? ParseValue()
            {
                var token = this.Current;
                switch (token.Type)
                {
                    case TokenType.String:
                        this.index++;
                        return token.Text;
                    case TokenType.Number:
                        this.index++;
                        return ParseNumber(token);
                    case TokenType.Identifier:
                        this.index++;
                        return token.Text.ToLowerInvariant() switch
                        {
                            "true" => true,
                            "false" => false,
                            "null" => null,
                            _ => throw Error($"Unexpected value '{token.Text}'. Strings must be quoted.", token),
                        };
                    case TokenType.LBracket:
                        return this.ParseList();
                    default:
                        throw Error($"Expected a value but found '{token.Text}'", token);
                }
            }

            private object?[] ParseList()
            {
                this.Expect(TokenType.LBracket);
                this.index++;

                var items = new List<object?>();
                if (this.Current.Type != TokenType.RBracket)
                {
                    items.Add(this.ParseValue());
                    while (this.Current.Type == TokenType.Comma)
                    {
                        this.index++;
                        items.Add(this.ParseValue());
                    }
                }

                this.Expect(TokenType.RBracket);
                this.index++;
                return items.ToArray();
            }

            private static object ParseNumber(Token token)
            {
                if (long.TryParse(token.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                {
                    return longValue;
                }

                if (double.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    return doubleValue;
                }

                throw Error($"'{token.Text}' is not a valid number.", token);
            }

            private static FormatException Error(string message, Token token)
            {
                return new FormatException($"Invalid filter expression at position {token.Position}: {message}.");
            }
        }

        private static class Lexer
        {
            internal static IReadOnlyList<Token> Tokenize(string input)
            {
                var tokens = new List<Token>();
                var i = 0;

                while (i < input.Length)
                {
                    var c = input[i];

                    if (char.IsWhiteSpace(c))
                    {
                        i++;
                        continue;
                    }

                    var start = i;

                    switch (c)
                    {
                        case '"':
                            tokens.Add(ReadString(input, ref i));
                            continue;
                        case '&':
                            Expect(input, i, '&');
                            tokens.Add(new Token(TokenType.And, "&&", start));
                            i += 2;
                            continue;
                        case '|':
                            Expect(input, i, '|');
                            tokens.Add(new Token(TokenType.Or, "||", start));
                            i += 2;
                            continue;
                        case '=':
                            Expect(input, i, '=');
                            tokens.Add(new Token(TokenType.Equal, "==", start));
                            i += 2;
                            continue;
                        case '!':
                            if (Peek(input, i + 1) == '=')
                            {
                                tokens.Add(new Token(TokenType.NotEqual, "!=", start));
                                i += 2;
                            }
                            else if (i + 1 < input.Length && (char.IsLetter(input[i + 1]) || input[i + 1] == '_'))
                            {
                                i++; // consume '!'
                                var keyword = ReadIdentifier(input, ref i);
                                var negatedToken = "!" + keyword.Text;
                                if (!FilterOperatorTokens.IsKeyword(negatedToken))
                                {
                                    throw new FormatException(
                                        $"Invalid filter expression at position {start}: '{negatedToken}' is not a valid operator. " +
                                        "Expected '!=', '!contains', '!startswith', '!endswith' or '!in'.");
                                }

                                tokens.Add(new Token(TokenType.Identifier, negatedToken, start));
                            }
                            else
                            {
                                throw new FormatException($"Invalid filter expression at position {start}: expected '=' or a keyword after '!'.");
                            }
                            continue;
                        case '>':
                            if (Peek(input, i + 1) == '=') { tokens.Add(new Token(TokenType.GreaterThanOrEqual, ">=", start)); i += 2; }
                            else { tokens.Add(new Token(TokenType.GreaterThan, ">", start)); i++; }
                            continue;
                        case '<':
                            if (Peek(input, i + 1) == '=') { tokens.Add(new Token(TokenType.LessThanOrEqual, "<=", start)); i += 2; }
                            else { tokens.Add(new Token(TokenType.LessThan, "<", start)); i++; }
                            continue;
                        case '(':
                            tokens.Add(new Token(TokenType.LParen, "(", start)); i++; continue;
                        case ')':
                            tokens.Add(new Token(TokenType.RParen, ")", start)); i++; continue;
                        case '[':
                            tokens.Add(new Token(TokenType.LBracket, "[", start)); i++; continue;
                        case ']':
                            tokens.Add(new Token(TokenType.RBracket, "]", start)); i++; continue;
                        case ',':
                            tokens.Add(new Token(TokenType.Comma, ",", start)); i++; continue;
                    }

                    if (c == '-' || char.IsDigit(c))
                    {
                        tokens.Add(ReadNumber(input, ref i));
                        continue;
                    }

                    if (char.IsLetter(c) || c == '_')
                    {
                        tokens.Add(ReadIdentifier(input, ref i));
                        continue;
                    }

                    throw new FormatException($"Invalid filter expression at position {i}: unexpected character '{c}'.");
                }

                tokens.Add(new Token(TokenType.End, "<end>", input.Length));
                return tokens;
            }

            private static Token ReadString(string input, ref int i)
            {
                var start = i;
                i++; // opening quote
                var builder = new StringBuilder();

                while (i < input.Length && input[i] != '"')
                {
                    if (input[i] == '\\' && i + 1 < input.Length)
                    {
                        i++;
                    }

                    builder.Append(input[i]);
                    i++;
                }

                if (i >= input.Length)
                {
                    throw new FormatException($"Invalid filter expression at position {start}: unterminated string literal.");
                }

                i++; // closing quote
                return new Token(TokenType.String, builder.ToString(), start);
            }

            private static Token ReadNumber(string input, ref int i)
            {
                var start = i;
                if (input[i] == '-')
                {
                    i++;
                }

                while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.'))
                {
                    i++;
                }

                return new Token(TokenType.Number, input.Substring(start, i - start), start);
            }

            private static Token ReadIdentifier(string input, ref int i)
            {
                var start = i;
                while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_' || input[i] == '.'))
                {
                    i++;
                }

                return new Token(TokenType.Identifier, input.Substring(start, i - start), start);
            }

            private static void Expect(string input, int i, char expected)
            {
                if (Peek(input, i + 1) != expected)
                {
                    throw new FormatException($"Invalid filter expression at position {i}: expected '{input[i]}{expected}'.");
                }
            }

            private static char Peek(string input, int i)
            {
                return i < input.Length ? input[i] : '\0';
            }
        }
    }
}
