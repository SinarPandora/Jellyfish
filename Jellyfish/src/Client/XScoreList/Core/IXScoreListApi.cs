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

    /// <summary>
    ///     Get total record counts
    /// </summary>
    /// <param name="showCancelled">Include cancelled records</param>
    /// <returns>Total counts</returns>
    [Get("/get_scores_count")]
    Task<XScoreApiResponse<XScoreCount>> GetScoreCount([AliasAs("show_cancelled")] bool showCancelled = false);

    /// <summary>
    ///     Get X-Score records with paging
    /// </summary>
    /// <param name="page">Current page, start from 1</param>
    /// <param name="perPage">Items per page</param>
    /// <param name="showCancelled">Show cancelled records</param>
    /// <param name="showInvalid">Show invalid records</param>
    /// <returns></returns>
    [Get("/get_scores")]
    Task<XScoreApiResponse<XScoreRecord>> GetScores(int page, [AliasAs("per_page")] int perPage,
        [AliasAs("show_cancelled")] bool showCancelled = false, [AliasAs("show_invalid")] bool showInvalid = false);
}
