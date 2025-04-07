namespace AnythingWorld.PathCreation 
{
	/// <summary>
	/// Enum that determines how VertexPath sampling is performed when the end of the path is reached.
	/// </summary>
	public enum EndOfPathInstruction
	{
		Loop, 
		Reverse, 
		Stop
	}
}
