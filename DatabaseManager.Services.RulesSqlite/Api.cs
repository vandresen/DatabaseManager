using DatabaseManager.Services.RulesSqlite.Services;
using DatabaseManager.Services.RulesSqlite.Models;
using Microsoft.AspNetCore.Builder;

namespace DatabaseManager.Services.RulesSqlite
{
    public static class Api
    {
        public static void ConfigureApi(this WebApplication app)
        {
            app.MapGet("/Rules", GetRules);
            app.MapGet("/Rules/{id}", GetRule);
            app.MapPost("/CreateDatabase", CreateRulesDatabase);
            app.MapPost("/CreateStandardRules", CreateStandardRules);
            app.MapPost("/Rules", SaveRules);
            app.MapPut("/Rules", UpdateRules);
            app.MapDelete("/Rules", DeleteRules);
            app.MapGet("/PredictionSet", GetPredictionSets);
            app.MapGet("/PredictionSet/{name}", GetPredictionSet);
            app.MapPost("/PredictionSet", SavePredictionSet);
            app.MapPut("/PredictionSet", UpdatePredictionSet);
            app.MapDelete("/PredictionSet", DeletePredictionSet);
        }

        private static Task DeletePredictionSet(HttpContext context)
        {
            throw new NotImplementedException();
        }

        private static Task UpdatePredictionSet(HttpContext context)
        {
            throw new NotImplementedException();
        }

        private static Task SavePredictionSet(HttpContext context)
        {
            throw new NotImplementedException();
        }

        private static Task GetPredictionSet(HttpContext context)
        {
            throw new NotImplementedException();
        }

        private static Task GetPredictionSets(IRuleAccess ra)
        {
            throw new NotImplementedException();
        }

        private static async Task<IResult> DeleteRules(int id, IRuleAccess ra)
        {
            ResponseDto response = new();
            try
            {
                await ra.DeleteRule(id);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"DeleteRules: Could not delete rule, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> UpdateRules(RuleModelDto rule, IRuleAccess ra)
        {
            ResponseDto response = new();
            try
            {
                await ra.CreateUpdateRule(rule);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"SaveRules: Could not save rules, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> SaveRules(RuleModelDto rule, IRuleAccess ra)
        {
            ResponseDto response = new();
            try
            {
                await ra.CreateUpdateRule(rule);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"SaveRules: Could not save rules, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> GetRules(IRuleAccess ra)
        {
            ResponseDto response = new();
            try
            {
                var rules = await ra.GetRules();
                response.Result = rules;
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"GetRules: Could not get rules, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> GetRule(int id, IRuleAccess ra)
        {
            ResponseDto response = new();
            try
            {
                var rule = await ra.GetRule(id);
                response.Result = rule;
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"GetRule: Could not get rule, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
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
                string newString = $"CreateRulesDatabase: Could not create database, {ex}";
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
