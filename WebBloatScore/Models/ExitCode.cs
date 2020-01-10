namespace WebBloatScore.Models
{
    public enum ExitCode
    {
        Success = 0,
        SuccessTimeout = 1,
        SuccessNotOptimized = 2,
        Fail = 3,
        FailTimeout = 4
    }
}