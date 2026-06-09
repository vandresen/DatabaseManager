using DatabaseManager.ServerLessClient.Models;
using ProtoBuf.Meta;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DatabaseManager.ServerLessClient.Services
{
    public class CopilotService : ICopilotService
    {
        private readonly ILLMBaseService _llm;
        private readonly IConfiguration _config;
        private readonly IDatabaseManagementService _dmService;
        private readonly ILogger<CopilotService> _logger;
        private readonly string _apiKey;

        private readonly string _systemPrompt;
        private readonly string _canonicalExamplesPrompt;

        private CopilotSchemaContext? _cachedSchema;
        private DateTime _schemaLoaded;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public CopilotService(
            ILLMBaseService llm,
            IConfiguration config,
            ILogger<CopilotService> logger,
            IDatabaseManagementService dmService)
        {
            _llm = llm;
            _config = config;
            _dmService = dmService;
            _logger = logger;

            _apiKey = SD.OpenAIKey;
            if (string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("AI key is not configured.");

            _systemPrompt = PromptLoader.Load("DatabaseManager.ServerLessClient.AIPrompts.RuleCopilotSystemPrompt.txt");
            _canonicalExamplesPrompt = PromptLoader.Load("DatabaseManager.ServerLessClient.AIPrompts.CanonicalRuleExamples.txt");
        }

        public async Task<string> ExplainRuleAsync(RuleModel? rule, string? question = null, CancellationToken ct = default)
        {
            try
            {
                string userContent = rule != null
        ? $"""
          Explain mode. You are explaining a data quality rule to a geologist or engineer.
          Do NOT return JSON. Answer in plain English only, 2-4 sentences.
          Here is the rule to explain:
          - Rule Key: {rule.RuleKey}
          - Rule Name: {rule.RuleName}
          - Data Type: {rule.DataType}
          - Field: {rule.DataAttribute}
          - Rule Type: {rule.RuleType}
          - Function: {rule.RuleFunction}
          - Parameters: {rule.RuleParameters}
          - Description: {rule.RuleDescription}
          """
        : question ?? "Explain how the rule system works.";

                var schema = await GetSchema();
                var schemaPrompt = BuildSchemaPrompt(schema);

                var finalSystemPrompt = $"""
{_systemPrompt}

{schemaPrompt}

{_canonicalExamplesPrompt}
""";

                var requestBody = new
                {
                    model = "gpt-5.4-mini",
                    temperature = 0.3,
                    messages = new object[]
                    {
                        new { role = "system", content = finalSystemPrompt },
                        new
                        {
                            role = "user",
                            content = userContent  // ← use the carefully constructed prompt
                        }
                    }
                };

                var headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {_apiKey}" }
                };

                string rawResponse = await _llm.SendRawAsync(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/chat/completions",
                    requestBody,
                    headers,
                    ct);

                using var document = JsonDocument.Parse(rawResponse);

                return document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "No explanation returned.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed explaining rule.");
                throw;
            }
        }

        public async Task<List<RuleModel>> GenerateRulesAsync(string prompt, CancellationToken ct = default)
        {
            try
            {
                var schema = await GetSchema();

                var schemaPrompt = BuildSchemaPrompt(schema);

                var finalSystemPrompt = $"""
{_systemPrompt}

{schemaPrompt}

{_canonicalExamplesPrompt}
""";

                var requestBody = new
                {
                    model = "gpt-5.4-mini",
                    temperature = 0.1,
                    messages = new object[]
                    {
                new
                {
                    role = "system",
                    content = finalSystemPrompt
                },
                new
                {
                    role = "user",
                    content = prompt
                }
                    }
                };

                var headers = new Dictionary<string, string>
        {
            {
                "Authorization",
                $"Bearer {_apiKey}"
            }
        };

                string rawResponse = await _llm.SendRawAsync(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/chat/completions",
                    requestBody,
                    headers,
                    ct);

                _logger.LogInformation(
                    "OpenAI raw response: {Response}",
                    rawResponse);

                // Parse OpenAI response envelope
                using var document =
                    JsonDocument.Parse(rawResponse);

                string? content =
                    document.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return new List<RuleModel>();
                }

                _logger.LogInformation(
                    "OpenAI content: {Content}",
                    content);

                // Deserialize generated rules
                var rules =
                    JsonSerializer.Deserialize<List<RuleModel>>(content, _jsonOptions);

                return rules ?? new List<RuleModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed generating rules.");

                throw;
            }
        }

        public async Task<string> ReviewRulesetAsync(List<RuleModel> rules, string question,  CancellationToken ct = default)
        {
            try
            {
                var schema = await GetSchema();
                var schemaPrompt = BuildSchemaPrompt(schema);

                var finalSystemPrompt = $"""
{_systemPrompt}

{schemaPrompt}

{_canonicalExamplesPrompt}
""";

                var rulesetJson = JsonSerializer.Serialize(rules);

                var requestBody = new
                {
                    model = "gpt-5.4-mini",
                    temperature = 0.2,
                    messages = new object[]
                    {
                        new { role = "system", content = finalSystemPrompt },
                        new
                        {
                            role = "user",
                            content = $"""
                Review mode. You are analysing a data quality ruleset for a geologist or engineer.
                Do NOT return JSON. Answer in plain English only.
                Be specific — reference actual RuleKeys and RuleNames from the ruleset.
                
                Ruleset: {rulesetJson}
                
                Question: {question}
                """
                        }
                    }
                };

                var headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {_apiKey}" }
                };

                string rawResponse = await _llm.SendRawAsync(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/chat/completions",
                    requestBody,
                    headers,
                    ct);

                using var document = JsonDocument.Parse(rawResponse);

                return document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "No review returned.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed reviewing ruleset.");
                throw;
            }
        }

        public IAsyncEnumerable<string> StreamAsync(string userMessage, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        private static class PromptLoader
        {
            public static string Load(string resourceName)
            {
                var assembly = Assembly.GetExecutingAssembly();

                using Stream? stream =
                    assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                    throw new Exception(
                        $"Prompt resource '{resourceName}' not found.");

                using StreamReader reader = new(stream);

                return reader.ReadToEnd();
            }
        }

        private async Task<CopilotSchemaContext> GetSchema()
        {
            if (_cachedSchema != null && DateTime.UtcNow - _schemaLoaded < TimeSpan.FromHours(1))
            {
                return _cachedSchema;
            }

            ResponseDto response =
                await _dmService.GetDataAccessDef<ResponseDto>();

            if (!response.IsSuccess)
            {
                throw new InvalidOperationException(
                    "Cannot get data access definition");
            }

            string json =
                response.Result?.ToString()
                ?? throw new InvalidOperationException(
                    "Schema response was empty");

            var definitions =
                JsonSerializer.Deserialize<List<DataAccessDef>>(
                    json,
                    _jsonOptions);

            if (definitions == null)
            {
                throw new InvalidOperationException(
                    "Failed to deserialize schema definition");
            }

            var schema = new CopilotSchemaContext();

            foreach (var definition in definitions)
            {
                var dataType = new CopilotDataType
                {
                    Name = definition.DataType
                };

                // Keys
                if (!string.IsNullOrWhiteSpace(definition.Keys))
                {
                    dataType.Keys = definition.Keys
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => k.Trim())
                        .ToList();
                }

                // Constants
                if (!string.IsNullOrWhiteSpace(definition.Constants))
                {
                    var constants =
                        definition.Constants.Split(
                            ';',
                            StringSplitOptions.RemoveEmptyEntries);

                    foreach (var constant in constants)
                    {
                        var parts = constant.Split('=');

                        if (parts.Length == 2)
                        {
                            dataType.Constants[
                                parts[0].Trim()] =
                                parts[1].Trim();
                        }
                    }
                }

                // Fields
                foreach (var field in definition.AttributeTypes)
                {
                    dataType.Fields.Add(new CopilotField
                    {
                        Name = field.Key,
                        Type = field.Value.ToString().ToLowerInvariant()
                    });
                }

                schema.DataTypes.Add(dataType);
            }

            _cachedSchema = schema;
            _schemaLoaded = DateTime.UtcNow;
            return schema;
        }

        public async Task<string> AskQuestionAsync(string question, CancellationToken ct = default)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-5.4-mini",
                    temperature = 0.3,
                    messages = new object[]
                    {
                new
                {
                    role = "system",
                    content = _systemPrompt  // system prompt only — no schema, no examples
                },
                new
                {
                    role = "user",
                    content = $"Explain mode. Answer this question in plain English for a geologist or engineer: {question}"
                }
                    }
                };

                var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {_apiKey}" }
        };

                string rawResponse = await _llm.SendRawAsync(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/chat/completions",
                    requestBody,
                    headers,
                    ct);

                using var document = JsonDocument.Parse(rawResponse);

                return document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "I'm unable to answer that question right now.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed answering question.");
                throw;
            }
        }

        private string BuildSchemaPrompt(CopilotSchemaContext schema)
        {
            var sb = new StringBuilder();

            sb.AppendLine("""
════════════════════════════════════════════════════════
KNOWN SCHEMA — AUTHORITATIVE FIELD NAMES
════════════════════════════════════════════════════════
CRITICAL: You MUST use ONLY the exact field names listed below for each DataType.
Do NOT substitute, rename, or infer field names from your own knowledge.
If a field name is not listed here, it does not exist in this system.
════════════════════════════════════════════════════════
""");

            foreach (var dataType in schema.DataTypes)
            {
                sb.AppendLine(dataType.Name);

                if (dataType.Keys.Any())
                {
                    sb.AppendLine(
                        $"Keys: {string.Join(", ", dataType.Keys)}");
                }

                sb.AppendLine("Fields:");

                foreach (var field in dataType.Fields)
                {
                    sb.AppendLine(
                        $"- {field.Name} ({field.Type})");
                }

                if (dataType.Constants.Any())
                {
                    sb.AppendLine("Constants:");

                    foreach (var constant in dataType.Constants)
                    {
                        sb.AppendLine(
                            $"- {constant.Key} = {constant.Value}");
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
