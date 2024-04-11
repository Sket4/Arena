
using NLog;
using NLog.Targets;

[Target("Unity")]
public class UnityConsoleLogTarget : TargetWithLayout
{
    protected override void Write(LogEventInfo logEvent)
    {
        var message = Layout.Render(logEvent);

        if (logEvent.Level == LogLevel.Info)
        {
            UnityEngine.Debug.Log(message);
        }
        else if(logEvent.Level == LogLevel.Error)
        {
            if (logEvent.Exception != null)
            {
                UnityEngine.Debug.LogException(logEvent.Exception);
            }
            else
            {
                UnityEngine.Debug.LogError(message);
            }
        }
        else if(logEvent.Level == LogLevel.Warn)
        {
            UnityEngine.Debug.LogWarning(message);
        }
        else
        {
            UnityEngine.Debug.Log(message);
        }
    }
}
