using DatabaseManager.Services.RulesSqlite.Services;
using DatabaseManager.Services.RulesSqlite.Models;

namespace DatabaseManager.Services.RulesSqlite
{
    public static class Api
    {
        public static void ConfigureApi(this WebApplication app)
        {
            //app.MapGet("/Index", GetIndexes);
            //app.MapGet("/Index/{id}", GetIndex);
            app.MapPost("/CreateDatabase", CreateRulesDatabase);
            app.MapPost("/CreateStandardRules", CreateStandardRules);
            //app.MapGet("/GetDescendants/{id}", GetDescendants);
            //app.MapGet("/GetNeighbors/{id}", GetNeighbors);
            //app.MapGet("/DmIndexes", GetDmIndexes);
            //app.MapGet("/Project", GetProjects);
            //app.MapPost("/Project", CreateProject);
            //app.MapDelete("/Project", DeleteProject);
        }

        private static async Task<IResult> CreateRulesDatabase(IRuleAccess ra)
        {
            ResponseDto response = new();
            try
            {
                await ra.CreateDatabaseRules();
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"CreateRulesDatabase: Could not not create database, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> CreateStandardRules(IRuleAccess ra)
        {
            ResponseDto response = new();
            try
            {
                await ra.InitializeStandardRules();
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"CreateStandardRules: Could not create standard rules in database, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }
    }
}
