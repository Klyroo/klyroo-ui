namespace Blux.Templates.Content;

/// <summary>A content file that cannot be used (bad front matter, two files with the same address). The message names the file.</summary>
public sealed class ContentException(string message, Exception? inner = null) : Exception(message, inner);
