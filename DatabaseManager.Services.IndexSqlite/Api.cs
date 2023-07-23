using Azure;
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
            app.MapGet("/GetDescendants/{id}", GetDescendants);
            app.MapGet("/GetNeighbors/{id}", GetNeighbors);
            app.MapGet("/DmIndexes", GetDmIndexes);
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
            ResponseDto response = new();
            try
            {
                var results = await idxAccess.GetIndex(id);
                if (results == null) 
                {
                    string newString = $"GetIndex: Index with id {id} could not be found";
                    response.ErrorMessages.Insert(0, newString);
                }
                response.IsSuccess = true;
                response.Result = results;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"GetIndex: Could not get Index with id {id}, {ex}";
                response.ErrorMessages.Insert(0, newString);
            }
            return Results.Ok(response);
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

        private static async Task<IResult> GetNeighbors(int id, IIndexAccess idxAccess)
        {
            try
            {
                return Results.Ok(await idxAccess.GetNeighbors(id));
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

        private static async Task<IResult> GetDmIndexes(int? id, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                if (!id.HasValue) id = 1;
                var result = await idxAccess.GetDmIndexes((int)id);
                response.IsSuccess = true;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"GetDmIndexes: Could not get DM Indexes, {ex}";
                response.ErrorMessages.Insert(0, newString);
            }
            return Results.Ok(response);
        }
    }
}
