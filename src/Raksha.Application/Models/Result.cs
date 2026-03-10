namespace Raksha.Application.Models
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string Message { get; }

        protected Result(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static Result Success(string message = "") => new(true, message);
        public static Result Failure(string message) => new(false, message);
    }

    public class Result<T> : Result
    {
        public T? Data { get; }

        private Result(bool isSuccess, string message, T? data = default)
            : base(isSuccess, message) => Data = data;

        public static Result<T> Success(T data, string message = "") => new(true, message, data);
        public new static Result<T> Failure(string message) => new(false, message);
    }
}
