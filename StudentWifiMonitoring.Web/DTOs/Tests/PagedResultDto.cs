namespace StudentWifiMonitoring.Web.DTOs.Tests;

/// <summary>
/// Generiek data transfer object voor gepagineerde resultaten.
/// </summary>
/// <typeparam name="T">Type van de items in het resultaat.</typeparam>
public class PagedResultDto<T>
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
