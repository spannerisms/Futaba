namespace FutabaCLI;

/// <summary>
/// Registers or removes the OS-level command shortcut and .futaba file association.
/// </summary>
internal interface IAssociationRegistrar {
	int Register();
	int Unregister();
}
