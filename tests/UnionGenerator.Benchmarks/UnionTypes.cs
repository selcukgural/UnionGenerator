namespace UnionGenerator.Benchmarks;

/// <summary>
/// Shared union types used across benchmark classes.
/// These are manually created union types following the UnionGenerator pattern.
/// </summary>
public static class SharedUnionTypes
{
    // These will be used by all benchmark classes
}

/// <summary>
/// A simple 2-type union for benchmarking.
/// </summary>
/// <typeparam name="T0">First type alternative.</typeparam>
/// <typeparam name="T1">Second type alternative.</typeparam>
public readonly struct Result<T0, T1>
{
    private readonly T0? _value0;
    private readonly T1? _value1;
    private readonly int _index;

    private Result(int index, T0? value0, T1? value1)
    {
        _index = index;
        _value0 = value0;
        _value1 = value1;
    }

    /// <summary>
    /// Creates a union from T0.
    /// </summary>
    public static Result<T0, T1> FromT0(T0 value) => new(0, value, default);

    /// <summary>
    /// Creates a union from T1.
    /// </summary>
    public static Result<T0, T1> FromT1(T1 value) => new(1, default, value);

    /// <summary>
    /// Implicit conversion from T0.
    /// </summary>
    public static implicit operator Result<T0, T1>(T0 value) => FromT0(value);

    /// <summary>
    /// Implicit conversion from T1.
    /// </summary>
    public static implicit operator Result<T0, T1>(T1 value) => FromT1(value);

    /// <summary>
    /// Gets whether this union holds a T0 value.
    /// </summary>
    public bool IsT0 => _index == 0;

    /// <summary>
    /// Gets whether this union holds a T1 value.
    /// </summary>
    public bool IsT1 => _index == 1;

    /// <summary>
    /// Gets the T0 value (throws if not T0).
    /// </summary>
    public T0 AsT0 => IsT0 ? _value0! : throw new InvalidOperationException("Not T0");

    /// <summary>
    /// Gets the T1 value (throws if not T1).
    /// </summary>
    public T1 AsT1 => IsT1 ? _value1! : throw new InvalidOperationException("Not T1");

    /// <summary>
    /// Tries to get the T0 value.
    /// </summary>
    public bool TryGetT0(out T0 value)
    {
        if (IsT0)
        {
            value = _value0!;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Tries to get the T1 value.
    /// </summary>
    public bool TryGetT1(out T1 value)
    {
        if (IsT1)
        {
            value = _value1!;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Pattern matches on the union value.
    /// </summary>
    public TResult Match<TResult>(Func<T0, TResult> f0, Func<T1, TResult> f1)
    {
        return _index switch
        {
            0 => f0(_value0!),
            1 => f1(_value1!),
            _ => throw new InvalidOperationException("Invalid state")
        };
    }

    /// <summary>
    /// Executes an action based on the union value.
    /// </summary>
    public void Switch(Action<T0> f0, Action<T1> f1)
    {
        switch (_index)
        {
            case 0:
                f0(_value0!);
                break;
            case 1:
                f1(_value1!);
                break;
            default:
                throw new InvalidOperationException("Invalid state");
        }
    }
}

/// <summary>
/// A 4-type union for benchmarking.
/// </summary>
/// <typeparam name="T0">First type alternative.</typeparam>
/// <typeparam name="T1">Second type alternative.</typeparam>
/// <typeparam name="T2">Third type alternative.</typeparam>
/// <typeparam name="T3">Fourth type alternative.</typeparam>
public readonly struct Result4<T0, T1, T2, T3>
{
    private readonly T0? _value0;
    private readonly T1? _value1;
    private readonly T2? _value2;
    private readonly T3? _value3;
    private readonly int _index;

    private Result4(int index, T0? value0, T1? value1, T2? value2, T3? value3)
    {
        _index = index;
        _value0 = value0;
        _value1 = value1;
        _value2 = value2;
        _value3 = value3;
    }

    /// <summary>
    /// Creates a union from T0.
    /// </summary>
    public static Result4<T0, T1, T2, T3> FromT0(T0 value) => new(0, value, default, default, default);

    /// <summary>
    /// Creates a union from T1.
    /// </summary>
    public static Result4<T0, T1, T2, T3> FromT1(T1 value) => new(1, default, value, default, default);

    /// <summary>
    /// Creates a union from T2.
    /// </summary>
    public static Result4<T0, T1, T2, T3> FromT2(T2 value) => new(2, default, default, value, default);

    /// <summary>
    /// Creates a union from T3.
    /// </summary>
    public static Result4<T0, T1, T2, T3> FromT3(T3 value) => new(3, default, default, default, value);
}

/// <summary>
/// An 8-type union for complex benchmarking scenarios.
/// </summary>
/// <typeparam name="T0">First type alternative.</typeparam>
/// <typeparam name="T1">Second type alternative.</typeparam>
/// <typeparam name="T2">Third type alternative.</typeparam>
/// <typeparam name="T3">Fourth type alternative.</typeparam>
/// <typeparam name="T4">Fifth type alternative.</typeparam>
/// <typeparam name="T5">Sixth type alternative.</typeparam>
/// <typeparam name="T6">Seventh type alternative.</typeparam>
/// <typeparam name="T7">Eighth type alternative.</typeparam>
public readonly struct Union8<T0, T1, T2, T3, T4, T5, T6, T7>
{
    private readonly object? _value;
    private readonly int _index;

    private Union8(int index, object? value)
    {
        _index = index;
        _value = value;
    }

    /// <summary>
    /// Creates a union from T0.
    /// </summary>
    public static Union8<T0, T1, T2, T3, T4, T5, T6, T7> FromT0(T0 value) => new(0, value);

    /// <summary>
    /// Creates a union from T1.
    /// </summary>
    public static Union8<T0, T1, T2, T3, T4, T5, T6, T7> FromT1(T1 value) => new(1, value);

    /// <summary>
    /// Creates a union from T2.
    /// </summary>
    public static Union8<T0, T1, T2, T3, T4, T5, T6, T7> FromT2(T2 value) => new(2, value);

    /// <summary>
    /// Creates a union from T3.
    /// </summary>
    public static Union8<T0, T1, T2, T3, T4, T5, T6, T7> FromT3(T3 value) => new(3, value);

    /// <summary>
    /// Creates a union from T4.
    /// </summary>
    public static Union8<T0, T1, T2, T3, T4, T5, T6, T7> FromT4(T4 value) => new(4, value);

    /// <summary>
    /// Creates a union from T5.
    /// </summary>
    public static Union8<T0, T1, T2, T3, T4, T5, T6, T7> FromT5(T5 value) => new(5, value);

    /// <summary>
    /// Creates a union from T6.
    /// </summary>
    public static Union8<T0, T1, T2, T3, T4, T5, T6, T7> FromT6(T6 value) => new(6, value);

    /// <summary>
    /// Creates a union from T7.
    /// </summary>
    public static Union8<T0, T1, T2, T3, T4, T5, T6, T7> FromT7(T7 value) => new(7, value);

    /// <summary>
    /// Tries to get the T0 value.
    /// </summary>
    public bool TryGetT0(out T0 value)
    {
        if (_index == 0)
        {
            value = (T0)_value!;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Tries to get the T1 value.
    /// </summary>
    public bool TryGetT1(out T1 value)
    {
        if (_index == 1)
        {
            value = (T1)_value!;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Tries to get the T2 value.
    /// </summary>
    public bool TryGetT2(out T2 value)
    {
        if (_index == 2)
        {
            value = (T2)_value!;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Tries to get the T3 value.
    /// </summary>
    public bool TryGetT3(out T3 value)
    {
        if (_index == 3)
        {
            value = (T3)_value!;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Tries to get the T4 value.
    /// </summary>
    public bool TryGetT4(out T4 value)
    {
        if (_index == 4)
        {
            value = (T4)_value!;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Tries to get the T5 value.
    /// </summary>
    public bool TryGetT5(out T5 value)
    {
        if (_index == 5)
        {
            value = (T5)_value!;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Tries to get the T6 value.
    /// </summary>
    public bool TryGetT6(out T6 value)
    {
        if (_index == 6)
        {
            value = (T6)_value!;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Tries to get the T7 value.
    /// </summary>
    public bool TryGetT7(out T7 value)
    {
        if (_index == 7)
        {
            value = (T7)_value!;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Pattern matches on the union value.
    /// </summary>
    public TResult Match<TResult>(
        Func<T0, TResult> f0,
        Func<T1, TResult> f1,
        Func<T2, TResult> f2,
        Func<T3, TResult> f3,
        Func<T4, TResult> f4,
        Func<T5, TResult> f5,
        Func<T6, TResult> f6,
        Func<T7, TResult> f7)
    {
        return _index switch
        {
            0 => f0((T0)_value!),
            1 => f1((T1)_value!),
            2 => f2((T2)_value!),
            3 => f3((T3)_value!),
            4 => f4((T4)_value!),
            5 => f5((T5)_value!),
            6 => f6((T6)_value!),
            7 => f7((T7)_value!),
            _ => throw new InvalidOperationException("Invalid state")
        };
    }
}

/// <summary>
/// Struct-based union for allocation benchmarks.
/// </summary>
/// <typeparam name="T0">First type alternative.</typeparam>
/// <typeparam name="T1">Second type alternative.</typeparam>
public readonly struct ResultStruct<T0, T1>
{
    private readonly T0? _value0;
    private readonly T1? _value1;
    private readonly int _index;

    private ResultStruct(int index, T0? value0, T1? value1)
    {
        _index = index;
        _value0 = value0;
        _value1 = value1;
    }

    /// <summary>
    /// Creates a union from T0.
    /// </summary>
    public static ResultStruct<T0, T1> FromT0(T0 value) => new(0, value, default);

    /// <summary>
    /// Creates a union from T1.
    /// </summary>
    public static ResultStruct<T0, T1> FromT1(T1 value) => new(1, default, value);
}

