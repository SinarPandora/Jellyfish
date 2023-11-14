using Jellyfish.Client.XScoreList.Request;
using Jellyfish.Client.XScoreList.Response;
using Refit;

namespace Jellyfish.Client.XScoreList.Core;

/// <summary>
///     API client for XScore List
/// </summary>
public interface IXScoreListApi
{
    /// <summary>
    ///     Submit X Score record in kook
    /// </summary>
    /// <param name="request">Request object</param>
    /// <returns>Submit response</returns>
    [Post("/submit_in_kook")]
    Task<XScoreApiResponse<XScoreSubmitResponse>> Submit([Body] SubmitXScoreRequest request);

    /// <summary>
    ///     Get not validated X-Score records
    /// </summary>
    /// <returns>Not validated X-Score records</returns>
    [Get("/get_not_validated")]
    Task<NotValidatedXScore[]> GetNotValidatedRecords();
}
