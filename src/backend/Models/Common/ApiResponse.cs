namespace KhduSouvenirShop.API.Models.Common
{
    public class ApiResponse<T> where T : class?
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }

        public ApiResponse() { }

        public ApiResponse(T? data, string? message = null)
        {
            Success = true;
            Data = data;
            Message = message;
        }

        public static ApiResponse<T> SuccessResult(T? data, string? message = null)
        {
            return new ApiResponse<T>(data, message);
        }

        public static ApiResponse<T> FailureResult(List<string> errors, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = errors,
                Message = message
            };
        }

        public static ApiResponse<T> FailureResult(string error, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = new List<string> { error },
                Message = message
            };
        }
    }
}
