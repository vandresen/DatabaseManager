using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface ICopilotService
    {
        /// <summary>Generate one or more rule objects from a plain-English description.</summary>
        Task<List<RuleModel>> GenerateRulesAsync(string prompt, CancellationToken ct = default);

        /// <summary>Explain a single rule in plain English for domain users.</summary>
        Task<string> ExplainRuleAsync(RuleModel? rule, string? question = null, CancellationToken ct = default);

        /// <summary>Review a ruleset and answer a question about gaps / coverage.</summary>
        Task<string> ReviewRulesetAsync(List<RuleModel> rules, string question, CancellationToken ct = default);

        /// <summary>Stream a free-form copilot response token by token.</summary>
        IAsyncEnumerable<string> StreamAsync(string userMessage, CancellationToken ct = default);

        Task<string> AskQuestionAsync(string question, CancellationToken ct = default);

    }
}
