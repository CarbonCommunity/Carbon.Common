namespace Oxide.Core.Libraries;

public class LibraryFunction : Attribute
{
	public string Name { get; set; }

	public LibraryFunction()
	{

	}

	public LibraryFunction(string name)
	{
		Name = name;
	}
}
