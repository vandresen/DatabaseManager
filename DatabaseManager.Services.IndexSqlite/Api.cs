using Microsoft.AspNetCore.Mvc;

namespace DatabaseManager.Services.IndexSqlite
{
    public static class Api
    {
        public static void ConfigureApi(this WebApplication app)
        {
            app.MapGet("/Index", GetIndexes);
            app.MapGet("/Index/{id}", GetIndex);
            app.MapPost("/Index", SaveIndex);
            app.MapPost("/CreateDatabase", CreateIndexDatabase);
            app.MapPost("/BuildIndex", BuildIndex);
            app.MapGet("/GetDescendants/{id}", GetDescendants);
            app.MapGet("/GetNeighbors/{id}", GetNeighbors);
            app.MapGet("/DmIndexes", GetDmIndexes);
            app.MapGet("/QueryIndex", QueryIndexes);
            app.MapGet("/EntiretyIndexes", EntiretyIndexes);
            app.MapGet("/Project", GetProjects);
            app.MapPost("/Project", CreateProject);
            app.MapDelete("/Project", DeleteProject);
            app.MapPut("/Indexes", UpdateIndexes);
            app.MapGet("/api/indexes/search", SearchIndexes);
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

        private static async Task<IResult> SearchIndexes(
            [AsParameters] IndexSearchCriteria criteria,
            [FromQuery(Name = "project")] string? project,
            IIndexAccess idxAccess)
        {
            // 1. Validate that at least one search attribute is provided
            if (string.IsNullOrWhiteSpace(criteria.DataName) &&
                string.IsNullOrWhiteSpace(criteria.DataType) &&
                string.IsNullOrWhiteSpace(criteria.QCString))
            {
                return Results.BadRequest(new
                {
                    Error = "Invalid search parameters.",
                    Message = "You must provide at least one search filter: 'DataName', 'DataType', or 'QCString'."
                });
            }

            try
            {
                // 2. Sanitize optional project string
                string? sanitizedProject = project?.Trim();

                // 3. Forward the strongly-typed filters down to your repository
                var results = await idxAccess.SearchIndexes(criteria, sanitizedProject);

                return results == null ? Results.NotFound() : Results.Ok(results);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: "An unexpected error occurred while executing the index search.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        private static async Task<IResult> GetDescendants(int id, string project, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                var result = await idxAccess.GetDescendants(id, project);
                response.IsSuccess = true;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Insert(0, $"GetDescendants: Could not get descendents for id {id}, {ex}");
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> GetNeighbors(int id, string failRule, string depthAttribute, string project, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                var result = await idxAccess.GetNeighbors(id, failRule, depthAttribute, project);
                response.IsSuccess = true;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Insert(0, $"GetNeighbors: Could not get neighbors for id {id}, {ex}");
            }
            return Results.Ok(response);
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

        private static async Task<IResult> QueryIndexes(string project, string dataType, string qcString, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                var result = await idxAccess.QueriedIndexes(project, dataType, qcString);
                response.IsSuccess = true;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"QueryIndexes: Could not get Indexes, {ex}";
                response.ErrorMessages.Insert(0, newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> EntiretyIndexes(string project, string dataType, string entiretyName, string parentType, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                var result = await idxAccess.GetEntiretyIndexes(project, dataType, entiretyName, parentType);
                response.IsSuccess = true;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"EntiretyIndexes: Could not get Indexes, {ex}";
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
                        if (string.IsNullOrEmpty(startsWith))
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

        private static async Task<IResult> UpdateIndexes(List<IndexModel> indexes, string Name, string Project, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                await idxAccess.UpdateIndexes(indexes, Project); 
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"UpdateIndexes: Could not update indexes in project, {ex}";
                response.ErrorMessages.Insert(0, newString);
            }

            return Results.Ok(response);
        }

        private static async Task<IResult> SaveIndex(IndexModel index, string Project, IIndexAccess idxAccess)
        {
            ResponseDto response = new();
            try
            {
                var result = await idxAccess.InsertSingleIndex(index, Project);
                response.IsSuccess = true;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"SaveIndex: Could not insert index in project, {ex}";
                response.ErrorMessages.Insert(0, newString);
            }

            return Results.Ok(response);
        }
    }
}
