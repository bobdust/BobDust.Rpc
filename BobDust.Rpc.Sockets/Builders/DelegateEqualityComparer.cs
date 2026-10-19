class DelegateEqualityComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T, T, bool> _equals;
    private readonly Func<T, int> _getHashCode;

    public DelegateEqualityComparer(Func<T, T, bool> equals, Func<T, int> getHashCode)
    {
        _equals = equals ?? throw new ArgumentNullException(nameof(equals));
        _getHashCode = getHashCode ?? throw new ArgumentNullException(nameof(getHashCode));
    }

    public bool Equals(T x, T y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;
        return _equals(x, y);
    }

    public int GetHashCode(T obj)
    {
        if (obj == null) return 0;
        return _getHashCode(obj);
    }
}
