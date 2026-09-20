namespace SampleProject.Common;

public class ReferenceTypeConstrained<T> where T : class
{
}

public class ValueTypeConstrained<T> where T : struct
{
}

public class UnmanagedConstrained<T> where T : unmanaged
{
}

public class NotNullConstrained<T> where T : notnull
{
}
