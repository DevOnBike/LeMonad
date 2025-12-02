namespace DevOnBike.LeMonad
{
    /// <summary>
    /// Represents the result of an operation: Success (Value) or Failure (Error).
    /// Optimized for .NET 8+ (Zero-Allocation on the heap for the structure itself).
    /// </summary>
    public readonly record struct Result<TValue, TError>
    {
        private readonly TValue _value;
        private readonly TError _error;

        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the value. Throws an exception if the result is a failure.
        /// </summary>
        public TValue Value => IsSuccess
            ? _value
            : throw new InvalidOperationException($"Cannot access the Value of a Failure result. Error: {_error}");

        /// <summary>
        /// Gets the error. Throws an exception if the result is a success.
        /// </summary>
        public TError Error => !IsSuccess
                ? _error is not null
                    ? _error
                    : throw new InvalidOperationException("Result is in a 'default' state (error not initialized). Use Result.Failure(...)")
                : throw new InvalidOperationException($"Cannot access the Error of a Success result. Value: {_value}");

        // Private constructor ensures state consistency
        private Result(TValue value, TError error, bool isSuccess)
        {
            _value = value;
            _error = error;

            IsSuccess = isSuccess;
        }

        // --- Factory Methods ---

        public static Result<TValue, TError> Success(TValue value)
        {
            return new(value, default, true);
        }

        public static Result<TValue, TError> Failure(TError error)
        {
            return new(default, error, false);
        }

        // --- Implicit Operators (Syntactic Sugar) ---
        // Allows writing: return "My Error"; instead of return Result<...>.Failure("...");

        public static implicit operator Result<TValue, TError>(TValue value)
        {
            return Success(value);
        }

        public static implicit operator Result<TValue, TError>(TError error)
        {
            return Failure(error);
        }

        // --- Functional Core Methods ---

        /// <summary>
        /// Unwraps the result, forcing handling of both paths.
        /// </summary>
        public R Match<R>(Func<TValue, R> onSuccess, Func<TError, R> onFailure)
        {
            return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
        }

        /// <summary>
        /// Executes a side effect action for both paths (e.g., logging).
        /// </summary>
        public void Switch(Action<TValue> onSuccess, Action<TError> onFailure)
        {
            if (IsSuccess)
            {
                onSuccess(_value!);
                return;
            }

            onFailure(_error!);
        }

        /// <summary>
        /// Map (Select): Transforms the Value (T -> U) if success. The Error propagates unchanged.
        /// </summary>
        public Result<U, TError> Map<U>(Func<TValue, U> mapper)
        {
            if (!IsSuccess)
            {
                return Result<U, TError>.Failure(_error);
            }

            return Result<U, TError>.Success(mapper(_value));
        }

        /// <summary>
        /// MapError: Transforms the Error (E -> F) if failure. The Value propagates unchanged.
        /// Useful for mapping errors from a lower layer to a higher layer.
        /// </summary>
        public Result<TValue, F> MapError<F>(Func<TError, F> errorMapper)
        {
            if (IsSuccess)
            {
                return Result<TValue, F>.Success(_value!);
            }

            return Result<TValue, F>.Failure(errorMapper(_error!));
        }

        /// <summary>
        /// Bind (FlatMap): Chains operations that can return an error.
        /// </summary>
        public Result<U, TError> Bind<U>(Func<TValue, Result<U, TError>> binder)
        {
            if (!IsSuccess)
            {
                return Result<U, TError>.Failure(_error!);
            }

            return binder(_value!);
        }

        // --- LINQ Support (Query Syntax) ---
        // Enables syntax: from x in result ...

        public Result<U, TError> Select<U>(Func<TValue, U> selector)
        {
            return Map(selector);
        }

        public Result<R, TError> SelectMany<U, R>(
            Func<TValue, Result<U, TError>> binder,
            Func<TValue, U, R> projector)
        {
            if (!IsSuccess)
            {
                return Result<R, TError>.Failure(_error!);
            }

            // 1. Execute the binder (intermediate operation)
            var bound = binder(_value!);

            // 2. If the intermediate operation failed -> return its error
            if (!bound.IsSuccess)
            {
                return Result<R, TError>.Failure(bound.Error);
            }

            // 3. If both succeeded -> execute the projection (combine results)
            return Result<R, TError>.Success(projector(_value!, bound.Value));
        }
    }
}