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
            app.MapGet("/Project", GetProjects);
            app.MapPost("/Project", CreateProject);
            app.MapDelete("/Project", DeleteProject);
        }

        private static async Task<IResult> GetIndexes(string project, IIndexAccess idxAccess)
        {
            try
            {
                return Results.Ok(await idxAccess.GetIndexes(project));
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }

        private static async Task<IResult> GetIndex(int id, string project, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                var results = await idxAccess.GetIndex(id, project);
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

        private static async Task<IResult> GetDescendants(int id, string project, IIndexAccess idxAccess)
        {
            try
            {
                return Results.Ok(await idxAccess.GetDescendants(id, project));
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }

        private static async Task<IResult> GetNeighbors(int id, string project, IIndexAccess idxAccess)
        {
            try
            {
                return Results.Ok(await idxAccess.GetNeighbors(id, project));
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
            ResponseDto response = new();
            try
            {
                if (string.IsNullOrEmpty(idxParms.Taxonomy)) return Results.BadRequest("Taxonomy file is missing");
                await idxAccess.BuildIndex(idxParms);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"BuildIndex: Could not build index, {ex}";
                response.ErrorMessages.Insert(0, newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> GetDmIndexes(int? id, string project, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                if (string.IsNullOrEmpty(project)) project = "Default";
                if (!id.HasValue) id = 1;
                var result = await idxAccess.GetDmIndexes((int)id, project);
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

        private static async Task<IResult> GetProjects(IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                var results = await idxAccess.GetProjects();
                List<string> projects = new();
                foreach (var result in results) 
                {
                    int indexOfEnd = result.IndexOf("pdo_qc_index");

                    if (indexOfEnd != -1)
                    {
                        string startsWith = result.Substring(0, indexOfEnd);
                        if(string.IsNullOrEmpty(startsWith)) 
                        { 
                            projects.Add("Default");
                        }
                        else
                        {
                            projects.Add(startsWith[..^1]);
                        }
                    }
                }
                response.IsSuccess = true;
                response.Result = projects;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"GetProjects: Could not get projects, {ex}";
                response.ErrorMessages.Insert(0, newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> CreateProject(string project, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                if (string.IsNullOrEmpty(project) == true)
                {
                    string newString = $"CreateProject: Project name is missing";
                    response.ErrorMessages.Insert(0, newString);
                    response.IsSuccess = false;
                }
                else
                {
                    await idxAccess.CreateProject(project);
                    response.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"CreateProject: Could not create project, {ex}";
                response.ErrorMessages.Insert(0, newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> DeleteProject(string project, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                await idxAccess.DeleteProject(project);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"DeleteProject: Could not delete project, {ex}";
                response.ErrorMessages.Insert(0, newString);
            }
            return Results.Ok(response);
        }
    }
}
