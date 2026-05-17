using Microsoft.AspNetCore.Mvc;

namespace DatabaseManager.Services.IndexSqlite.Models
{
    public record IndexSearchCriteria(
    [FromQuery] string? DataName,
    [FromQuery] string? DataType,
    [FromQuery] string? QCString);
}
