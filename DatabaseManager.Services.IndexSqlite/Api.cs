using DatabaseManager.Services.IndexSqlite.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Threading.Tasks;

namespace DatabaseManager.Services.IndexSqlite
{
    public static class Api
    {
        public static void ConfigureApi(this WebApplication app)
        {
            app.MapGet("/Index", GetIndexes);
            app.MapGet("/Index/{id}", GetIndex);
            app.MapPost("/CreateDatabase", CreateIndexDatabase);
            app.MapPost("/BuildIndex", BuildIndex);
            app.MapPost("/GetDescendants/{id}", GetDescendants);
        }

        private static async Task<IResult> GetIndexes(IIndexAccess idxAccess)
        {
            try
            {
                return Results.Ok(await idxAccess.GetIndexes());
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }

        private static async Task<IResult> GetIndex(int id, IIndexAccess idxAccess)
        {
            try
            {
                var results = await idxAccess.GetIndex(id);
                if (results == null) return Results.NotFound();
                return Results.Ok(results);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }

        private static async Task<IResult> GetDescendants(int id, IIndexAccess idxAccess)
        {
            try
            {
                return Results.Ok(await idxAccess.GetDescendants(id));
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }

        private static async Task<IResult> CreateIndexDatabase(IIndexAccess idxAccess)
        {
            try
            {
                await idxAccess.CreateDatabaseIndex();
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }

        private static async Task<IResult> BuildIndex(BuildIndexParameters idxParms, IIndexAccess idxAccess, IDataSourceService dataSource)
        {
            try
            {
                if (string.IsNullOrEmpty(idxParms.TaxonomyFile)) return Results.BadRequest("Taxonomy file is missing");
                await idxAccess.BuildIndex(idxParms);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }
    }
}
