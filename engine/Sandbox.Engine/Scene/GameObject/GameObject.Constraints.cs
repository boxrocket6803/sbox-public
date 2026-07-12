namespace Sandbox;

public partial class GameObject
{
	/// <summary>
	/// List of BaseConstraint components that are attempting to control this object
	/// </summary>
	internal List<BaseConstraint> Constraints { get; set; }
}
