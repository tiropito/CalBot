namespace CalBot.Core;

/// <summary>
/// Приложение однопользовательское. Все данные пишутся под этим UserId.
/// Когда/если появится многопользовательность — UserId будет браться из claims.
/// </summary>
public static class AppConstants
{
public const long SingleUserId = 0L;
}