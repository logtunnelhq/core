// LogTunnel — Audience-specific changelog translator
// Copyright (C) 2026 LogTunnel contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using LogTunnel.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LogTunnel.Core.Llm;

/// <summary>
/// Composition-root helper that registers a single
/// <see cref="IChatCompletionService"/> in the DI container based on
/// <see cref="LlmOptions.Provider"/>. Self-hosters can switch between
/// Anthropic, OpenAI, and a local Ollama instance by editing the
/// <c>Llm</c> configuration section — no code changes required.
/// </summary>
/// <remarks>
/// This is a non-static class so it honours the project rule that
/// behavioural code should not live in static classes, but it is
/// instantiated directly at the composition root rather than resolved
/// from DI: it is glue, not a service. The same registrar is used by
/// LogTunnel.Api and LogTunnel.Cli so both hosts pick up identical
/// behaviour from a shared <c>Llm</c> configuration section.
/// </remarks>
public sealed class LlmConnectorRegistrar
{
    private const string DefaultOllamaEndpoint = "http://localhost:11434";

    /// <summary>
    /// Resolve <see cref="LlmOptions.Provider"/> against the
    /// <see cref="LlmProvider"/> enum and register the matching
    /// <see cref="IChatCompletionService"/> in <paramref name="services"/>.
    /// Throws on unknown providers and on missing required configuration
    /// so startup fails fast with an actionable message.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="options">Bound LLM configuration options.</param>
    public void Register(IServiceCollection services, LlmOptions options)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (options is null) throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.Model))
            throw new InvalidOperationException(
                "Llm:Model is required. Set the LLM__MODEL environment variable or 'Llm:Model' in configuration.");

        var provider = ParseProvider(options.Provider);

        switch (provider)
        {
            case LlmProvider.Anthropic:
                RegisterAnthropic(services, options);
                break;

            case LlmProvider.OpenAI:
                RegisterOpenAI(services, options);
                break;

            case LlmProvider.Ollama:
                RegisterOllama(services, options);
                break;

            default:
                // Should be unreachable because ParseProvider validates,
                // but the compiler can't see that and we want a clear
                // error if a new enum value is added without wiring.
                throw new InvalidOperationException(
                    $"Llm provider '{provider}' is recognised but not wired in {nameof(LlmConnectorRegistrar)}.");
        }
    }

    private static LlmProvider ParseProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new InvalidOperationException(
                $"Llm:Provider is required. Supported providers: {SupportedProviderList()}.");

        if (!Enum.TryParse<LlmProvider>(providerName, ignoreCase: true, out var provider) ||
            !Enum.IsDefined(typeof(LlmProvider), provider))
        {
            throw new InvalidOperationException(
                $"Unknown Llm:Provider '{providerName}'. Supported providers: {SupportedProviderList()}.");
        }

        return provider;
    }

    private static void RegisterAnthropic(IServiceCollection services, LlmOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException(
                "Llm:ApiKey is required for the Anthropic provider. Set the LLM__APIKEY environment variable.");

        services.AddSingleton<IChatCompletionService>(serviceProvider =>
            new AnthropicChatCompletionService(
                options.ApiKey,
                options.Model,
                serviceProvider.GetRequiredService<ILogger<AnthropicChatCompletionService>>()));
    }

    private static void RegisterOpenAI(IServiceCollection services, LlmOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException(
                "Llm:ApiKey is required for the OpenAI provider. Set the LLM__APIKEY environment variable.");

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            services.AddOpenAIChatCompletion(
                modelId: options.Model,
                apiKey: options.ApiKey);
        }
        else
        {
            services.AddOpenAIChatCompletion(
                modelId: options.Model,
                endpoint: new Uri(options.BaseUrl),
                apiKey: options.ApiKey);
        }
    }

    private static void RegisterOllama(IServiceCollection services, LlmOptions options)
    {
        var endpoint = new Uri(
            string.IsNullOrWhiteSpace(options.BaseUrl) ? DefaultOllamaEndpoint : options.BaseUrl);

        services.AddOllamaChatCompletion(
            modelId: options.Model,
            endpoint: endpoint);
    }

    private static string SupportedProviderList() =>
        string.Join(", ", Enum.GetNames<LlmProvider>());
}
