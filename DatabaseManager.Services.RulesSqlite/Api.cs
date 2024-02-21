using DatabaseManager.Services.RulesSqlite.Services;
using DatabaseManager.Services.RulesSqlite.Models;
using Microsoft.AspNetCore.Builder;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DatabaseManager.Services.RulesSqlite
{
    public static class Api
    {
        public static void ConfigureApi(this WebApplication app)
        {
            app.MapGet("/Rules", GetRules);
            app.MapGet("/Rule", GetRule);
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
            app.MapGet("/Function", GetFunctions);
            app.MapGet("/Function/{id}", GetFunction);
            app.MapPost("/Function", SaveFunction);
            app.MapPut("/Function", UpdateFunction);
            app.MapDelete("/Function", DeleteFunction);
        }

        private static async Task<IResult> DeleteFunction(int id, IFunctionAccess fa)
        {
            ResponseDto response = new();
            try
            {
                await fa.DeleteFunction(id);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"DeleteFunction: Could not delete function, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> UpdateFunction(RuleFunctionsDto function, IFunctionAccess fa)
        {
            ResponseDto response = new();
            try
            {
                await fa.CreateUpdateFunction(function);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"UpdateFunction: Could not update function, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> SaveFunction(RuleFunctionsDto function, IFunctionAccess fa)
        {
            ResponseDto response = new();
            try
            {
                await fa.CreateUpdateFunction(function);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"SaveRules: Could not save function, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> GetFunction(int id, IFunctionAccess fa)
        {
            ResponseDto response = new();
            try
            {
                RuleFunctionsDto functions = await fa.GetFunction(id);
                response.Result = functions;
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"GetFunction: Could not get function, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> GetFunctions(IFunctionAccess fa)
        {
            ResponseDto response = new();
            try
            {
                IEnumerable<RuleFunctionsDto> functions= await fa.GetFunctions();
                response.Result = functions;
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"GetFunctions: Could not get functions, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> DeletePredictionSet(int id, IPredictionSetAccess ps)
        {
            ResponseDto response = new();
            try
            {
                await ps.DeletePredictionDataSet(id);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"DeletePredictionSet: Could not delete prediction set, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> UpdatePredictionSet(PredictionSet predSet, IPredictionSetAccess ps)
        {
            ResponseDto response = new();
            try
            {
                var oldPredSets = await ps.GetPredictionDataSet(predSet.Name);
                if (oldPredSets == null) 
                {
                    response.IsSuccess = false;
                    string newString = $"Update PredictionSet: Predictions set does not exist";
                    if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                    response.ErrorMessages.Add(newString);
                }
                else 
                {
                    predSet.Id = oldPredSets.Id;
                    await ps.UpdatePredictionDataSet(predSet);
                    response.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"Save PredictionSet: Could not save prediction set, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> SavePredictionSet(PredictionSet predSet, IPredictionSetAccess ps)
        {
            ResponseDto response = new();
            try
            {
                await ps.SavePredictionDataSet(predSet);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"SavePredictionSet: Could not save prediction set, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> GetPredictionSet(string name, IPredictionSetAccess ps)
        {
            ResponseDto response = new();
            try
            {
                var predSets = await ps.GetPredictionDataSet(name);
                response.Result = predSets;
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"GetPredictionSet: Could not get prediction set, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
        }

        private static async Task<IResult> GetPredictionSets(IPredictionSetAccess ps)
        {
            ResponseDto response = new();
            try
            {
                var predSets = await ps.GetPredictionDataSets();
                response.Result = predSets;
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                string newString = $"GetPredictionSets: Could not get prediction sets, {ex}";
                if (response.ErrorMessages == null) response.ErrorMessages = new List<string>();
                response.ErrorMessages.Add(newString);
            }
            return Results.Ok(response);
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

        private static async Task<IResult> GetRule(int Id, IRuleAccess ra)
        {
            ResponseDto response = new();
            try
            {
                var rule = await ra.GetRule(Id);
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
