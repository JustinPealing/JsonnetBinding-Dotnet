namespace JsonnetBinding
{
    /// <summary>
    /// Callback used to load imports.
    /// </summary>
    /// <param name="baseDir">The directory containing the code that did the import.</param>
    /// <param name="rel">The path imported by the code.</param>
    /// <param name="foundHere">Set this to path to the file, absolute or relative to the process's CWD.  This is
    /// necessary so that imports from the content of the imported file can be resolved correctly. Only use when
    /// <paramref name="success"/> = true.</param>
    /// <param name="success">Set this to true to indicate success and false for failure.</param>
    /// <returns>The content of the imported file, or an error message.</returns>
    public delegate string ImportCallback(string baseDir, string rel, out string foundHere, out bool success);

    public delegate object NativeCallback(object[] args);
}