namespace Jellyfish.Client.XScoreList.Request;

/// <summary>
///     Submit X-Score record request
/// </summary>
public class SubmitXScoreRequest
{
    public SubmitXScoreRequest(int kookId, double score, string token)
    {
        KookId = kookId;
        Score = score;
        Token = token;
    }

    public int KookId { get; set; }
    public double Score { get; set; }
    public string Token { get; set; }
}
