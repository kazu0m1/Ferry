using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ferry
{
    internal sealed class SearchRequest
    {
        public string RootPath;
        public string Query;
        public string Mode;
        public bool ShowHidden;
    }

    internal static class SearchService
    {
        private static readonly CompareInfo JapaneseCompare = CultureInfo.GetCultureInfo("ja-JP").CompareInfo;
        private const CompareOptions NameCompareOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreWidth;
        public static Task SearchAsync(SearchRequest request, Action<string> onPathFound, CancellationToken token)
        {
            return Task.Run(delegate
            {
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                object gate = new object();
                try { SearchWindowsIndex(request, onPathFound, seen, gate, token); }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { Logger.Write("Indexed search unavailable: " + ex.Message); }

                if (token.IsCancellationRequested) return;
                try { DirectSearch(request, onPathFound, seen, gate, token); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Logger.Write("Direct search error: " + ex.ToString()); }
            }, token);
        }

        private static void SearchWindowsIndex(SearchRequest request, Action<string> callback, HashSet<string> seen, object gate, CancellationToken token)
        {
            if (!Directory.Exists(request.RootPath)) return;
            string where = BuildIndexWhere(request);
            if (string.IsNullOrEmpty(where)) return;
            string scope = ToScopeUri(request.RootPath);
            string sql = "SELECT System.ItemPathDisplay FROM SYSTEMINDEX WHERE SCOPE='" + SqlEscape(scope) + "' AND (" + where + ")";
            string connectionString = "Provider=Search.CollatorDSO;Extended Properties='Application=Ferry'";

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            using (OleDbCommand command = new OleDbCommand(sql, conn))
            {
                conn.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader != null && reader.Read())
                    {
                        token.ThrowIfCancellationRequested();
                        string path = reader.IsDBNull(0) ? null : Convert.ToString(reader.GetValue(0));
                        if (string.IsNullOrEmpty(path)) continue;
                        if (!File.Exists(path) && !Directory.Exists(path)) continue;
                        if (!request.ShowHidden && IsHidden(path)) continue;
                        if (!Matches(Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)), request.Query, request.Mode)) continue;
                        Emit(path, callback, seen, gate);
                    }
                }
            }
        }

        private static string BuildIndexWhere(SearchRequest request)
        {
            string query = (request.Query ?? string.Empty).Trim();
            if (query.Length == 0) return string.Empty;
            if (ContainsWildcard(query))
            {
                string like = EscapeLike(query).Replace("*", "%").Replace("?", "_");
                return "System.FileName LIKE '" + SqlEscape(like) + "'";
            }
            if (string.Equals(request.Mode, "StartsWith", StringComparison.OrdinalIgnoreCase))
                return "System.FileName LIKE '" + SqlEscape(EscapeLike(query)) + "%'";

            string[] terms = query.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < terms.Length; i++)
            {
                if (i > 0) sb.Append(" AND ");
                sb.Append("System.FileName LIKE '%");
                sb.Append(SqlEscape(EscapeLike(terms[i])));
                sb.Append("%'");
            }
            return sb.ToString();
        }

        private static void DirectSearch(SearchRequest request, Action<string> callback, HashSet<string> seen, object gate, CancellationToken token)
        {
            Stack<string> pending = new Stack<string>();
            pending.Push(request.RootPath);
            while (pending.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                string folder = pending.Pop();
                IEnumerable<string> entries;
                try { entries = Directory.EnumerateFileSystemEntries(folder); }
                catch (UnauthorizedAccessException) { continue; }
                catch (IOException) { continue; }
                catch { continue; }

                try
                {
                    foreach (string entry in entries)
                    {
                        token.ThrowIfCancellationRequested();
                        FileAttributes attrs;
                        try { attrs = File.GetAttributes(entry); }
                        catch { continue; }
                        bool hidden = (attrs & FileAttributes.Hidden) != 0;
                        bool directory = (attrs & FileAttributes.Directory) != 0;
                        bool reparse = (attrs & FileAttributes.ReparsePoint) != 0;

                        if ((request.ShowHidden || !hidden) && Matches(Path.GetFileName(entry.TrimEnd(Path.DirectorySeparatorChar)), request.Query, request.Mode))
                            Emit(entry, callback, seen, gate);

                        if (directory && !reparse && (request.ShowHidden || !hidden))
                            pending.Push(entry);
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }

        private static void Emit(string path, Action<string> callback, HashSet<string> seen, object gate)
        {
            bool add;
            lock (gate) add = seen.Add(Normalize(path));
            if (add && callback != null) callback(path);
        }

        public static bool Matches(string name, string query, string mode)
        {
            if (name == null) return false;
            query = (query ?? string.Empty).Trim();
            if (query.Length == 0) return true;
            if (ContainsWildcard(query)) return WildcardMatch(name, query);
            if (string.Equals(mode, "StartsWith", StringComparison.OrdinalIgnoreCase))
                return JapaneseCompare.IsPrefix(name, query, NameCompareOptions);

            string[] terms = query.Split(new char[] { ' ', '\t', '\u3000' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < terms.Length; i++)
                if (JapaneseCompare.IndexOf(name, terms[i], NameCompareOptions) < 0) return false;
            return true;
        }

        private static bool WildcardMatch(string text, string pattern)
        {
            // Normalize compatibility-width forms first so patterns such as ガ* also match
            // half-width names such as ｶﾞ..., while still keeping hiragana and katakana distinct.
            text = NormalizeWidth(text);
            pattern = NormalizeWidth(pattern);
            int t = 0, p = 0, star = -1, match = 0;
            while (t < text.Length)
            {
                if (p < pattern.Length && (pattern[p] == '?' || char.ToUpperInvariant(pattern[p]) == char.ToUpperInvariant(text[t]))) { p++; t++; }
                else if (p < pattern.Length && pattern[p] == '*') { star = p++; match = t; }
                else if (star != -1) { p = star + 1; t = ++match; }
                else return false;
            }
            while (p < pattern.Length && pattern[p] == '*') p++;
            return p == pattern.Length;
        }

        private static string NormalizeWidth(string value)
        {
            return (value ?? string.Empty).Normalize(NormalizationForm.FormKC);
        }

        private static bool ContainsWildcard(string value)
        {
            return value.IndexOf('*') >= 0 || value.IndexOf('?') >= 0;
        }

        private static string EscapeLike(string value)
        {
            // Windows Search LIKE supports %, _, [, ] as wildcards. Escape the common literals.
            return value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        }

        private static string SqlEscape(string value) { return value.Replace("'", "''"); }

        private static bool IsHidden(string path)
        {
            try { return (File.GetAttributes(path) & FileAttributes.Hidden) != 0; }
            catch { return false; }
        }

        private static string ToScopeUri(string path)
        {
            string full = Path.GetFullPath(path);
            if (!full.EndsWith(Path.DirectorySeparatorChar.ToString())) full += Path.DirectorySeparatorChar;
            return new Uri(full).AbsoluteUri;
        }

        private static string Normalize(string path)
        {
            try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant(); }
            catch { return path.ToUpperInvariant(); }
        }
    }
}
