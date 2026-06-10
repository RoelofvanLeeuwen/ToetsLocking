using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using System.Text;

namespace StudentWifiMonitoring.Web.Services
{
    public static class CsvExporter
    {
        public static async Task<string> ExportByTestAsync(AppDbContext db, int testSessionId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("StudentName,TestName,EventType,TimestampUtc");
            var rows = await db.Events.Where(e => e.TestSessionId == testSessionId)
                .Include(e => e.Student).Include(e => e.TestSession).OrderBy(e => e.Timestamp).ToListAsync();
            foreach (var e in rows)
            {
                sb.AppendLine($"{Escape(e.Student?.Name)},{Escape(e.TestSession?.Name)},{e.EventType},{e.Timestamp:o}");
            }
            return sb.ToString();
        }

        private static string Escape(string? v) => string.IsNullOrEmpty(v) ? "" : (v.Contains(',') ? $"\"{v.Replace("\"", "\"\"")}\"" : v);
    }
}
