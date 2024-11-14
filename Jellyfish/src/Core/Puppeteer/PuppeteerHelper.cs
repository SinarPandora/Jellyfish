using PuppeteerSharp;

namespace Jellyfish.Core.Puppeteer;

/// <summary>
///     Puppeteer Helper Functions
/// </summary>
public static class PuppeteerHelper
{
    /// <summary>
    ///     Get innerText for Dom Element by evaluating JavaScript
    /// </summary>
    /// <param name="elm">Dom element</param>
    /// <returns>The innerText value</returns>
    public static Task<string> InnerTextAsync(this IElementHandle elm)
    {
        return elm.EvaluateFunctionAsync<string>("e => e.innerText");
    }
}
