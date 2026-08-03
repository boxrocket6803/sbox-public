namespace Sandbox;

public static class CryptidUtility {
	public static void AddLogger( Action<LogEvent> logger ) => Logging.OnMessage += logger;
	public static void RemoveLogger( Action<LogEvent> logger ) => Logging.OnMessage -= logger;
}
