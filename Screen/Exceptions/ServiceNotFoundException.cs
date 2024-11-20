namespace Screen.Exceptions;

public class ServiceNotFoundException(string name) : Exception(name);