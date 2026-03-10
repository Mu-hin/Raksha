namespace Raksha.Application.Models
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public object? Data { get; }
        protected Result(bool isSuccess, string message, object? data = null)
        {
            IsSuccess = isSuccess;
            Message = message;
            Data = data;
        }

        public static Result Success(string message = "", object? data = null) => new(true, message, data);
        public static Result Failure(string message, object? data = null) => new(false, message, data);
    }

    public class Result<T> : Result
    {
        public new T? Data { get; }

        private Result(bool isSuccess, string message, T? data = default)
            : base(isSuccess, message, data) => Data = data;

        public static Result<T> Success(string message = "", T? data = default) => new(true, message, data);
        public static Result<T> Failure(string message, T? data = default) => new(false, message, data);
    }
}
