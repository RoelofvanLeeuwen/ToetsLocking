using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web.Data;
using StudentWifiMonitoring.Web.Domain;

namespace StudentWifiMonitoring.Web.Services;

/// <summary>
/// Service voor het beheren van toetssessies.
/// Bevat businesslogica voor het ophalen, filteren en pagineren van toetsen.
/// </summary>
public class TestManagementService
{
    private readonly AppDbContext _db;

    public TestManagementService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Haalt een gefilterde en gepagineerde lijst van toetssessies op.
    /// Actieve toetsen worden bovenaan getoond, overige van nieuw naar oud.
    /// </summary>
    /// <param name="searchTerm">Optionele zoekterm voor toetsnaam (case-insensitive).</param>
    /// <param name="pageNumber">Paginanummer (1-based).</param>
    /// <param name="pageSize">Aantal items per pagina.</param>
    /// <returns>Gepagineerd resultaat met toetssessies en totaal aantal items.</returns>
    public async Task<PagedResult<TestSession>> GetPagedTestsAsync(string? searchTerm, int pageNumber, int pageSize)
    {
        var now = DateTime.UtcNow; // Voor SQL-vertaalbare vergelijking
        var query = _db.TestSessions.AsQueryable();

        // Filter op zoekterm (case-insensitive)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(searchLower));
        }

        // Tel totaal aantal resultaten
        var totalCount = await query.CountAsync();

        // Sorteer: actieve toetsen bovenaan (SQL-vertaalbare expressie), dan nieuw naar oud
        var items = await query
            .OrderByDescending(t => t.StartTime <= now && t.EndTime >= now) // IsActive logica, SQL-vertaalbaar
            .ThenByDescending(t => t.StartTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TestSession>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}

/// <summary>
/// Representeert een gepagineerd resultaat met metadata.
/// </summary>
/// <typeparam name="T">Type van de items in het resultaat.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// De items voor de huidige pagina.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Totaal aantal items over alle pagina's.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Huidige paginanummer (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Aantal items per pagina.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Totaal aantal pagina's.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Of er een vorige pagina bestaat.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Of er een volgende pagina bestaat.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}