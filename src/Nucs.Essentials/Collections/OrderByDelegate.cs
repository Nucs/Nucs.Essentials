namespace Nucs.Collections;

public delegate TOrderBy OrderByDelegate<in T, out TOrderBy>(T item);