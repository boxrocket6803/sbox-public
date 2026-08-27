using Sandbox.Engine;

namespace Sandbox;

public static class CryptidUtility {
	public static void AddLogger( Action<LogEvent> logger ) => Logging.OnMessage += logger;
	public static void RemoveLogger( Action<LogEvent> logger ) => Logging.OnMessage -= logger;

	public static bool ShiftDown() => InputRouter.IsButtonDown(NativeEngine.ButtonCode.KEY_LSHIFT) || InputRouter.IsButtonDown(NativeEngine.ButtonCode.KEY_RSHIFT);
}
